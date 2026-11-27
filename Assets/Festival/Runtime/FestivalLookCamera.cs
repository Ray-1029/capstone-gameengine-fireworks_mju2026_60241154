using UnityEngine;

namespace LanternFestival
{
    [RequireComponent(typeof(Camera))]
    public sealed class FestivalLookCamera : MonoBehaviour
    {
        [Range(.2f,8)] public float mouseSensitivity=2.2f;
        public float keyboardDegreesPerSecond=48;
        public float minimumPitch=-35;
        public float maximumPitch=82;
        public float minimumFov=32;
        public float maximumFov=76;
        public bool showHint=true;
        Camera view;
        Quaternion homeRotation;
        float homeFov,yaw,pitch;
        bool initialized,dragging;
        float hintUntil;
        bool helpOpen;
        Font helpFont;
        GUIStyle helpStyle;
        public bool IsLooking => dragging;

        void Awake() { Initialize(); }
        public void Initialize()
        {
            if(initialized) return;
            view=GetComponent<Camera>(); homeRotation=transform.rotation; homeFov=view.fieldOfView;
            initialized=true; ReadAngles(); hintUntil=Time.unscaledTime+9;
        }
        void ReadAngles()
        {
            yaw=transform.eulerAngles.y;
            pitch=-Mathf.DeltaAngle(0,transform.eulerAngles.x);
        }
        public void RotateView(float horizontal,float vertical)
        {
            Initialize(); yaw=Mathf.Repeat(yaw+horizontal,360);
            pitch=Mathf.Clamp(pitch+vertical,minimumPitch,maximumPitch);
            transform.rotation=Quaternion.Euler(-pitch,yaw,0);
        }
        public void Zoom(float delta)
        {
            Initialize(); view.fieldOfView=Mathf.Clamp(view.fieldOfView-delta,minimumFov,maximumFov);
        }
        public void ResetView()
        {
            Initialize(); ReleaseCursor(); transform.rotation=homeRotation; view.fieldOfView=homeFov;
            ReadAngles(); hintUntil=Time.unscaledTime+5;
        }
        void Update()
        {
            if(Input.GetMouseButtonDown(1)) { dragging=true; Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false; }
            if(Input.GetMouseButtonUp(1)||Input.GetKeyDown(KeyCode.Escape)) ReleaseCursor();
            if(Input.GetKeyDown(KeyCode.C)||Input.GetKeyDown(KeyCode.Home)) ResetView();
            if(Input.GetKeyDown(KeyCode.F1)) { helpOpen=!helpOpen;hintUntil=0; }
            if(dragging) RotateView(Input.GetAxisRaw("Mouse X")*mouseSensitivity,Input.GetAxisRaw("Mouse Y")*mouseSensitivity);
            float x=(Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.LeftArrow)?1:0);
            float y=(Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.DownArrow)?1:0);
            if(x!=0||y!=0) RotateView(x*keyboardDegreesPerSecond*Time.unscaledDeltaTime,y*keyboardDegreesPerSecond*Time.unscaledDeltaTime);
            if(Input.mouseScrollDelta.y!=0) Zoom(Input.mouseScrollDelta.y*3);
        }
        void ReleaseCursor()
        {
            if(!dragging) return;
            dragging=false; Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
        }
        void OnApplicationFocus(bool focus) { if(!focus) ReleaseCursor(); }
        void OnDisable() { ReleaseCursor(); }
        void OnGUI()
        {
            if(!showHint) return;
            if(helpStyle==null)
            {
                helpFont=Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Arial"},14);
                helpStyle=new GUIStyle(GUI.skin.label) {font=helpFont,fontSize=14,wordWrap=true,normal={textColor=new Color(1,.91f,.74f)}};
            }
            if(GUI.Button(new Rect(Screen.width-172,18,154,28),"Camera / Help  [F1]")) { helpOpen=!helpOpen;hintUntil=0; }
            if(!helpOpen && Time.unscaledTime>hintUntil) return;
            float width=Mathf.Min(400,Screen.width-36);
            Rect panel=new Rect(Screen.width-width-18,54,width,162);
            GUI.Box(panel,GUIContent.none);
            GUI.Label(new Rect(panel.x+12,panel.y+10,width-24,145),"Game 화면을 클릭한 후 조작하세요.\n우클릭 드래그 / 방향키: 둘러보기\n마우스 휠: 확대·축소\nC / Home: 기본 구도 복귀\n우클릭 놓기 / Esc: 마우스 잠금 해제\nSpace: 일시정지   R: 처음부터",helpStyle);
        }
        void OnDestroy() { if(helpFont) Destroy(helpFont); }
    }
}
