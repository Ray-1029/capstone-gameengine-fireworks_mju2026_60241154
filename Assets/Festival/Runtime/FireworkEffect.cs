using UnityEngine;

namespace LanternFestival
{
    // These are genuine world-space Unity particles. Positions and fine trail samples
    // are evaluated from absolute music time; no accumulated frame time is used.
    public sealed class FireworkEffect : MonoBehaviour
    {
        public FireworkShape shape;
        [ColorUsage(true, true)] public Color color = new Color(1, .66f, .18f, 1);
        public ParticleSystem cores;
        public ParticleSystem tails;
        public ParticleSystem sparks;
        [Range(40, 600)] public int rayCount = 180;
        public float speed = 9;
        public float lifetime = 4.4f;
        public float gravity = 1.2f;
        public float drag = .36f;
        public float trailLength = .62f;
        public float particleWidth = .065f;
        [Header("Parade variations")]
        [Min(.1f)] public float secondaryDelay = 1.1f;
        [Range(4,18)] public int satelliteCount = 12;
        [ColorUsage(true,true)] public Color secondaryColor = new Color(.902f,.196f,.153f,1);
        [Range(2,16)] public float glitterFrequency = 9;
        public bool HasBurst => shape != FireworkShape.Fan && shape != FireworkShape.Comet;
        public float VisibleDuration => shape == FireworkShape.MultiBreak ? Mathf.Max(lifetime,secondaryDelay+satelliteCount*.035f+2.8f) : HasBurst ? lifetime + .15f : lifetime;
        ParticleSystem.Particle[] coreBuffer = new ParticleSystem.Particle[2048];
        ParticleSystem.Particle[] tailBuffer = new ParticleSystem.Particle[32000];
        ParticleSystem.Particle[] sparkBuffer = new ParticleSystem.Particle[8192];
        Vector3[] directions;
        float[] variation;
        FireworkCue cue;
        int coreN, tailN, sparkN;

        public void Prepare(FireworkCue value)
        {
            cue = value;
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            directions = new Vector3[rayCount];
            variation = new float[rayCount];
            var rng = new System.Random(cue.seed);
            for (int i = 0; i < rayCount; i++)
            {
                float y = 1 - 2 * (i + .5f) / rayCount;
                float r = Mathf.Sqrt(1 - y * y);
                float a = i * 2.39996323f + (float)rng.NextDouble() * .065f;
                directions[i] = new Vector3(r * Mathf.Cos(a), y, r * Mathf.Sin(a));
                variation[i] = .84f + (float)rng.NextDouble() * .26f;
                if(shape==FireworkShape.Star)
                {
                    // A pattern shell made of burning points, with physical variation.
                    float edge=Mathf.Repeat((i+(float)rng.NextDouble()-.5f)/rayCount*10,10); int vertex=Mathf.FloorToInt(edge);
                    float a0=Mathf.PI*.5f+vertex*Mathf.PI/5;
                    float a1=a0+Mathf.PI/5;
                    float r0=vertex%2==0?1:.43f, r1=vertex%2==0?.43f:1;
                    directions[i]=Vector3.Lerp(new Vector3(Mathf.Cos(a0)*r0,Mathf.Sin(a0)*r0,0),new Vector3(Mathf.Cos(a1)*r1,Mathf.Sin(a1)*r1,0),edge-vertex);
                    directions[i]+=new Vector3((float)rng.NextDouble()-.5f,(float)rng.NextDouble()-.5f,((float)rng.NextDouble()-.5f)*3)*.045f;
                    variation[i]=.89f+(float)rng.NextDouble()*.23f;
                    if(i%11==0) directions[i]=SphereDirection(i,rayCount)*.35f;
                }
                else if(shape==FireworkShape.DoubleRing)
                {
                    float theta=((i/2)+(float)rng.NextDouble()-.5f)/(rayCount/2)*Mathf.PI*2;
                    Vector3 ring=new Vector3(Mathf.Cos(theta),Mathf.Sin(theta),((float)rng.NextDouble()-.5f)*.14f);
                    directions[i]=(i%2==0?Quaternion.Euler(12,23,22):Quaternion.Euler(-12,-48,-30))*ring;
                    variation[i]=.90f+(float)rng.NextDouble()*.20f;
                }
            }
            foreach (var ps in new[] { cores, tails, sparks })
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(false); ps.Pause(false);
            }
        }

        void Put(ParticleSystem.Particle[] buffer, ref int n, Vector3 p, float width, Color c, Vector3 velocity)
        {
            c.a*=cue==null?1:cue.brightness;
            if (n >= buffer.Length || c.a < .002f || width < .001f) return;
            buffer[n++] = new ParticleSystem.Particle { position = p, startSize = width,
                startColor = c, remainingLifetime = 100, startLifetime = 100, velocity = velocity, randomSeed = (uint)n };
        }

        public void Evaluate(double musicTime)
        {
            if (cue == null) return;
            coreN = tailN = sparkN = 0;
            float age = (float)(musicTime - cue.LaunchTime);
            if (age >= 0)
            {
                if (HasBurst)
                {
                    if (age < cue.riseTime) DrawRocket(age);
                    else DrawBurst(age - cue.riseTime);
                }
                else DrawGround(age);
            }
            cores.SetParticles(coreBuffer, coreN);
            tails.SetParticles(tailBuffer, tailN);
            sparks.SetParticles(sparkBuffer, sparkN);
            cores.Simulate(0,false,false,false);
            tails.Simulate(0,false,false,false);
            sparks.Simulate(0,false,false,false);
        }

        Vector3 RocketPoint(float age)
        {
            float u = Mathf.Clamp01(age / Mathf.Max(.001f, cue.riseTime));
            return cue.launchPosition + Vector3.up * (cue.height * (1 - Mathf.Pow(1 - u, 1.35f)));
        }

        void DrawRocket(float age)
        {
            var head = RocketPoint(age);
            Put(coreBuffer, ref coreN, head, .14f, new Color(1,.92f,.64f,.95f), Vector3.up * 5);
            for (int k = 0; k < 42; k++)
            {
                float delay = k * .012f;
                if (age < delay) break;
                var p = RocketPoint(age - delay);
                p.x += Mathf.Sin(k * 3.7f + age * 20) * .025f;
                var c = new Color(1,.57f,.12f, .65f * Mathf.Pow(1-k/42f, 1.8f));
                Put(tailBuffer, ref tailN, p, .047f, c, Vector3.up);
            }
        }

        Vector3 Flight(Vector3 v, float t)
        {
            float d = Mathf.Max(.001f, drag);
            float f = (1 - Mathf.Exp(-d * t)) / d;
            return v * f + Vector3.down * (.5f * gravity * t * t);
        }

        void DrawBurst(float age)
        {
            if(shape==FireworkShape.MultiBreak) { DrawMultiBreak(age); return; }
            if(shape==FireworkShape.Glitter) { DrawGlitter(age); return; }
            if(shape==FireworkShape.Star || shape==FireworkShape.DoubleRing) { DrawPattern(age); return; }
            if(shape==FireworkShape.Palm) { DrawPalm(age); return; }
            if(shape==FireworkShape.Peony) { DrawPeony(age); return; }
            if(shape==FireworkShape.Crossette) { DrawCrossette(age); return; }
            if(shape==FireworkShape.FallingLeaves) { DrawLeaves(age); return; }
            if (age > lifetime) return;
            Vector3 origin = cue.launchPosition + Vector3.up * cue.height;
            float scale = cue.scale;
            float flash = Mathf.Clamp01(1-age/.16f);
            if (flash > 0)
            {
                Put(sparkBuffer, ref sparkN, origin, (1.2f + age*9) * scale, new Color(1,.93f,.71f,flash*.9f), Vector3.zero);
                Put(coreBuffer, ref coreN, origin, .6f*scale, Color.white * new Color(1,1,1,flash), Vector3.zero);
            }
            int steps = shape == FireworkShape.Willow ? 40 : 22;
            for (int i = 0; i < rayCount; i++)
            {
                float life = lifetime * (.83f + .17f * variation[i]);
                if (age > life) continue;
                var v = directions[i] * speed * scale * variation[i];
                if (shape == FireworkShape.Willow) { v.y = (v.y + speed * scale * .36f) * .8f; }
                float fade = Mathf.Pow(Mathf.Clamp01(1 - age/life), .6f);
                float twinkle = .72f + .28f * Mathf.Sin(age*19 + i*5.2f) * Mathf.Sin(age*13 + i*2.1f);
                var p = origin + Flight(v, age);
                var tangent = v*Mathf.Exp(-drag*age) + Vector3.down*gravity*age;
                var rayColor=shape==FireworkShape.DoubleRing && i%2==1?secondaryColor:color;
                var hot = Color.Lerp(rayColor, new Color(1,.96f,.79f,1), .68f);
                hot.a = fade * twinkle;
                Put(coreBuffer, ref coreN, p, Mathf.Max(.16f,particleWidth * scale * 2), hot, tangent);
                float length = Mathf.Min(age, trailLength);
                for (int k = 1; k <= steps; k++)
                {
                    float q = k/(float)steps;
                    float t = age - length*q;
                    var tail = origin + Flight(v, t);
                    // Older willow embers continue falling; chrysanthemum rays remain crisp.
                    if (shape == FireworkShape.Willow) tail.y -= length*q*.8f;
                    var c = rayColor;
                    c.a = fade * (1-q) * .73f;
                    Put(tailBuffer, ref tailN, tail, Mathf.Max(.085f,particleWidth*scale)*(.95f-.55f*q), c, tangent);
                }
                if (shape == FireworkShape.Willow && age > .7f && i % 3 == 0)
                {
                    var ember = p + Vector3.down*(.3f+.4f*Mathf.Sin(i))*age;
                    var c = color; c.a = fade*.55f*twinkle;
                    Put(sparkBuffer, ref sparkN, ember, .035f*scale,c,Vector3.down);
                }
            }
        }

        static Vector3 SphereDirection(int i,int count)
        {
            float y=1-2*(i+.5f)/count, r=Mathf.Sqrt(Mathf.Max(0,1-y*y)), a=i*2.39996323f;
            return new Vector3(r*Mathf.Cos(a),y,r*Mathf.Sin(a));
        }
        float Hash(int i,int salt=0)
        {
            unchecked { uint n=(uint)(i*747796405+cue.seed*289133645+salt*277803737); n=(n^(n>>16))*2246822519u; n=(n^(n>>13))*3266489917u; return (n&0xffffff)/16777215f; }
        }
        Vector3 PatternFlight(Vector3 velocity,float age,int i)
        {
            float resistance=Mathf.Max(.01f,drag*(.72f+Hash(i,1)*.7f));
            return velocity*((1-Mathf.Exp(-resistance*age))/resistance)
                +new Vector3(.06f*age*age,-.5f*gravity*(.75f+Hash(i,2)*.7f)*age*age,0);
        }
        void DrawPattern(float age)
        {
            if(age>lifetime) return;
            Vector3 origin=cue.launchPosition+Vector3.up*cue.height;
            Flash(origin,age,1.4f*cue.scale,color);
            for(int i=0;i<rayCount;i++)
            {
                float t=age-Hash(i,3)*.045f, life=lifetime*(.58f+.42f*Hash(i,4));
                if(t<0 || t>life) continue;
                Vector3 v=directions[i]*speed*cue.scale*variation[i];
                float fade=Mathf.Pow(Mathf.Clamp01(1-t/life),.65f);
                float twinkle=.68f+.32f*Mathf.Sin(t*(11+Hash(i,6)*17)+i*2.31f);
                Color baseColor=shape==FireworkShape.DoubleRing && i%2==1?secondaryColor:color;
                Color burning=Color.Lerp(baseColor,new Color(1,.29f,.045f),Mathf.Clamp01(t/life-.28f)*.45f);
                Color hot=Color.Lerp(burning,new Color(1,.94f,.73f),Mathf.Clamp01(1-t/life)*.65f);
                hot.a=fade*twinkle;
                Vector3 point=origin+PatternFlight(v,t,i);
                Put(coreBuffer,ref coreN,point,(.30f+.16f*Hash(i,7))*Mathf.Sqrt(cue.scale),hot,Vector3.zero);
                float length=Mathf.Min(t,trailLength*(.4f+.8f*Hash(i,8)));
                for(int k=1;k<=10;k++)
                {
                    if(Hash(i*13+k,9)<.24f) continue;
                    float q=k/10f, previous=t-length*q;
                    Color ember=burning; ember.a=fade*(1-q)*(.30f+.34f*Hash(i*17+k,10));
                    Vector3 p=origin+PatternFlight(v,previous,i);
                    Vector3 tangent=(PatternFlight(v,previous+.01f,i)-PatternFlight(v,previous,i))*100;
                    Put(tailBuffer,ref tailN,p,(.08f+.045f*Hash(i,11))*(1-.55f*q),ember,tangent);
                }
                if(t>.8f && i%3==0)
                {
                    var ember=burning; ember.a=fade*.32f;
                    Put(sparkBuffer,ref sparkN,point+Vector3.down*t*(.1f+Hash(i,12)*.35f),.07f,ember,Vector3.down);
                }
            }
        }
        void DrawPalm(float age)
        {
            if(age>lifetime) return;
            Vector3 origin=cue.launchPosition+Vector3.up*cue.height;
            Flash(origin,age,1.5f*cue.scale,color);
            for(int i=0;i<11;i++)
            {
                float a=i*Mathf.PI*2/11+Hash(i)*.18f;
                Vector3 v=new Vector3(Mathf.Cos(a)*.83f,.66f+Hash(i,1)*.4f,Mathf.Sin(a)*.62f)*speed*cue.scale;
                float life=lifetime*(.78f+.22f*Hash(i,2)),fade=Mathf.Pow(Mathf.Clamp01(1-age/life),.6f);
                SmallTrail(origin,v,age,trailLength,color,fade,.14f*cue.scale,40);
                for(int k=0;k<16;k++)
                {
                    float lag=k*.07f, t=age-lag;
                    if(t<0) break;
                    Vector3 p=origin+Flight(v,t)+new Vector3((Hash(i*31+k,4)-.5f)*lag,-lag*lag*.8f,(Hash(i*31+k,5)-.5f)*lag);
                    var c=color;c.a=fade*(1-k/16f)*(.4f+.3f*Mathf.Sin(age*16+k));
                    Put(sparkBuffer,ref sparkN,p,.085f*cue.scale,c,Vector3.down);
                }
            }
        }
        void DrawPeony(float age)
        {
            if(age>lifetime) return;
            Vector3 origin=cue.launchPosition+Vector3.up*cue.height;
            Flash(origin,age,1.6f*cue.scale,color);
            for(int i=0;i<rayCount;i++)
            {
                float life=lifetime*(.65f+.35f*Hash(i)),fade=Mathf.Pow(Mathf.Clamp01(1-age/life),.7f);
                bool pistil=i%4==0;
                Vector3 v=directions[i]*speed*cue.scale*variation[i]*(pistil?.42f:1);
                Color tint=pistil?color:Color.Lerp(secondaryColor,color,Hash(i,1)*.2f);
                SmallTrail(origin,v,age,.06f+Hash(i,2)*.09f,tint,fade,.12f*cue.scale,4);
            }
        }
        void DrawCrossette(float age)
        {
            if(age>lifetime) return;
            Vector3 origin=cue.launchPosition+Vector3.up*cue.height;
            Flash(origin,age,1.5f*cue.scale,color);
            for(int i=0;i<9;i++)
            {
                Vector3 direction=SphereDirection(i,9);
                Vector3 carrier=direction*speed*cue.scale;
                float delay=secondaryDelay+Hash(i)*.18f;
                if(age<delay) { SmallTrail(origin,carrier,age,.40f,color,.9f,.13f,20);continue; }
                float t=age-delay, fade=Mathf.Pow(Mathf.Clamp01(1-t/2.7f),.7f);
                Vector3 centre=origin+Flight(carrier,delay);
                Vector3 axisA=Vector3.Cross(direction,Vector3.up).normalized;
                Vector3 axisB=Vector3.Cross(direction,axisA).normalized;
                Flash(centre,t,.65f*cue.scale,color);
                for(int branch=0;branch<4;branch++)
                for(int ember=0;ember<3;ember++)
                {
                    Vector3 axis=branch==0?axisA:branch==1?-axisA:branch==2?axisB:-axisB;
                    Vector3 v=carrier*.18f+axis*speed*cue.scale*(.37f+ember*.025f);
                    SmallTrail(centre,v,t,.48f,color,fade*(1-ember*.2f),.08f,15);
                }
            }
        }
        void DrawLeaves(float age)
        {
            if(age>lifetime) return;
            Vector3 origin=cue.launchPosition+Vector3.up*cue.height;
            Flash(origin,age,.35f*cue.scale,color);
            for(int i=0;i<rayCount;i++)
            {
                float life=lifetime*(.72f+.28f*Hash(i));
                float fade=Mathf.Pow(Mathf.Clamp01(1-age/life),.65f);
                Vector3 v=directions[i]*speed*cue.scale;
                Vector3 p=origin+Flight(v,age);
                p.x+=Mathf.Sin(age*1.5f+i)*age*.14f;
                var c=Color.Lerp(color,new Color(1,.38f,.08f),age/lifetime*.55f);
                c.a=fade*(.65f+.35f*Mathf.Sin(age*7+i*2.3f));
                Put(coreBuffer,ref coreN,p,.20f*Mathf.Sqrt(cue.scale),c,Vector3.zero);
                for(int k=1;k<=10;k++)
                {
                    float lag=k*.038f;if(age<lag)break;
                    var tail=c;tail.a*=.55f*(1-k/11f);
                    Vector3 q=origin+Flight(v,age-lag);q.x+=Mathf.Sin((age-lag)*1.5f+i)*(age-lag)*.14f;
                    Put(tailBuffer,ref tailN,q,.06f*cue.scale,tail,Vector3.down);
                }
            }
        }
        void Flash(Vector3 point,float age,float size,Color tint)
        {
            float fade=Mathf.Clamp01(1-age/.13f);
            tint=Color.Lerp(tint,Color.white,.7f); tint.a=fade;
            Put(sparkBuffer,ref sparkN,point,size,tint,Vector3.zero);
        }
        void SmallTrail(Vector3 origin,Vector3 velocity,float age,float length,Color tint,float fade,float width,int steps)
        {
            Vector3 tangent=velocity*Mathf.Exp(-drag*age)+Vector3.down*gravity*age;
            var hot=Color.Lerp(tint,Color.white,.67f); hot.a=fade;
            Put(coreBuffer,ref coreN,origin+Flight(velocity,age),width*2.1f,hot,tangent);
            for(int k=1;k<=steps;k++)
            {
                float q=k/(float)steps, t=Mathf.Max(0,age-Mathf.Min(age,length)*q);
                var c=tint; c.a=fade*(1-q)*.8f;
                Put(tailBuffer,ref tailN,origin+Flight(velocity,t),width*(1-.55f*q),c,tangent);
            }
        }
        void DrawMultiBreak(float age)
        {
            if(age>VisibleDuration) return;
            Vector3 origin=cue.launchPosition+Vector3.up*cue.height;
            Flash(origin,age,2.4f*cue.scale,color);
            int children=Mathf.Clamp(satelliteCount,4,18);
            for(int group=0;group<children;group++)
            {
                Vector3 carrier=SphereDirection(group,children)*speed*cue.scale;
                float delay=secondaryDelay+group*.035f;
                if(age<delay) SmallTrail(origin,carrier,age,.42f,color,.95f,.13f,18);
                else
                {
                    float childAge=age-delay;
                    if(childAge>2.8f) continue;
                    Vector3 centre=origin+Flight(carrier,delay);
                    Color tint=group%3==0?secondaryColor:color;
                    Flash(centre,childAge,1.3f*cue.scale,tint);
                    float fade=Mathf.Pow(Mathf.Clamp01(1-childAge/2.8f),.65f);
                    for(int i=0;i<38;i++)
                    {
                        Vector3 v=SphereDirection(i,38)*speed*.36f*cue.scale;
                        SmallTrail(centre,v,childAge,.48f,tint,fade,.085f,13);
                    }
                }
            }
        }
        void DrawGlitter(float age)
        {
            if(age>lifetime) return;
            Vector3 origin=cue.launchPosition+Vector3.up*cue.height;
            Flash(origin,age,1.4f*cue.scale,color);
            for(int i=0;i<rayCount;i++)
            {
                float localAge=age-(variation[i]-.84f)*.4f;
                if(localAge<0) continue;
                float remaining=Mathf.Clamp01(1-localAge/lifetime);
                float pulse=Mathf.Pow(.5f+.5f*Mathf.Sin(localAge*glitterFrequency*Mathf.PI*2+i*2.39996f),10);
                float flare=Mathf.Max(Mathf.Clamp01(1-localAge/.18f),pulse);
                Vector3 v=directions[i]*speed*cue.scale*variation[i];
                var tint=Color.Lerp(color,Color.white,.7f*flare); tint.a=remaining*(.035f+.965f*flare);
                Put(sparkBuffer,ref sparkN,origin+Flight(v,localAge),(.12f+.34f*flare)*cue.scale,tint,Vector3.zero);
                if(flare>.45f) SmallTrail(origin,v,localAge,.065f,color,remaining*flare,.065f,3);
            }
        }

        void DrawGround(float age)
        {
            if (age > lifetime) return;
            bool comet = shape == FireworkShape.Comet;
            float heightMultiplier=1;
            if(cue.height>0)
            {
                float naturalHeight=0;
                for(int i=0;i<=40;i++) naturalHeight=Mathf.Max(naturalHeight,Flight(Vector3.up*speed*cue.scale,lifetime*i/40f).y);
                heightMultiplier=cue.height/Mathf.Max(.01f,naturalHeight);
            }
            int streams = comet ? 3 : 13;
            int emissions = comet ? 1 : 6;
            for (int s = 0; s < streams; s++)
            for (int j = 0; j < emissions; j++)
            {
                float t = age - j*.045f;
                if (t < 0) continue;
                float angle = Mathf.Lerp(comet ? -8 : -58, comet ? 8 : 58, s/(float)(streams-1))*Mathf.Deg2Rad;
                var v = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), .045f*Mathf.Sin(s*2))*speed*cue.scale;
                float fade = Mathf.Pow(Mathf.Clamp01(1-t/lifetime),.8f);
                var offset=Flight(v,t); offset.y*=heightMultiplier;
                var p = cue.launchPosition + offset;
                var hot=Color.Lerp(color,Color.white,.63f); hot.a=fade;
                Put(coreBuffer,ref coreN,p,particleWidth*1.7f,hot,v);
                int steps = comet ? 80 : 34;
                for(int k=1;k<=steps;k++)
                {
                    float q=k/(float)steps;
                    float dt=Mathf.Min(t,trailLength)*q;
                    var c=color; c.a=fade*(1-q)*.85f;
                    var tailOffset=Flight(v,t-dt); tailOffset.y*=heightMultiplier;
                    Put(tailBuffer,ref tailN,cue.launchPosition+tailOffset,particleWidth*(1-.7f*q),c,v);
                }
            }
        }
    }
}
