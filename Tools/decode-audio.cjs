const fs = require('fs');
const path = require('path');
const { chromium } = require('C:/Program Files/WindowsApps/OpenAI.CodexPrimaryRuntime.v26-1123-915-0_26.1123.915.0_x64__3k8sg7r9htsxt/dependencies/node/node_modules/playwright');

(async () => {
  const input = process.argv[2];
  const output = process.argv[3];
  fs.mkdirSync(output, { recursive: true });
  const browser = await chromium.launch({ executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe', headless: true });
  try {
    const page = await browser.newPage();
    const result = await page.evaluate(async (base64) => {
      const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
      const context = new OfflineAudioContext(2, 1, 48000);
      const decoded = await context.decodeAudioData(bytes.buffer);
      const channels = Array.from({length: decoded.numberOfChannels}, (_, c) => decoded.getChannelData(c));
      const pcm = new Uint8Array(decoded.length * decoded.numberOfChannels * 2);
      const view = new DataView(pcm.buffer);
      for (let i = 0; i < decoded.length; i++) {
        for (let c = 0; c < channels.length; c++) {
          const value = Math.max(-1, Math.min(1, channels[c][i]));
          view.setInt16((i * channels.length + c) * 2, value < 0 ? value * 32768 : value * 32767, true);
        }
      }
      const chunks = [];
      for (let start = 0; start < pcm.length; start += 32768) chunks.push(String.fromCharCode(...pcm.subarray(start, start + 32768)));
      return {sampleRate: decoded.sampleRate, channels: decoded.numberOfChannels, frames: decoded.length, duration: decoded.duration, pcm: btoa(chunks.join(''))};
    }, fs.readFileSync(input).toString('base64'));
    const pcm = Buffer.from(result.pcm, 'base64');
    const header = Buffer.alloc(44);
    header.write('RIFF'); header.writeUInt32LE(36 + pcm.length, 4); header.write('WAVEfmt ', 8);
    header.writeUInt32LE(16, 16); header.writeUInt16LE(1, 20); header.writeUInt16LE(result.channels, 22);
    header.writeUInt32LE(result.sampleRate, 24); header.writeUInt32LE(result.sampleRate * result.channels * 2, 28);
    header.writeUInt16LE(result.channels * 2, 32); header.writeUInt16LE(16, 34); header.write('data', 36); header.writeUInt32LE(pcm.length, 40);
    fs.writeFileSync(path.join(output, 'ado_show_decoded.wav'), Buffer.concat([header, pcm]));
    delete result.pcm;
    result.source = path.resolve(input);
    result.decoder = 'Chrome OfflineAudioContext.decodeAudioData; 48000 Hz PCM; original leading/trailing decoded samples preserved';
    result.sourceSha256 = require('crypto').createHash('sha256').update(fs.readFileSync(input)).digest('hex');
    fs.writeFileSync(path.join(output, 'audio-metadata.json'), JSON.stringify(result, null, 2));
    console.log(JSON.stringify(result, null, 2));
  } finally { await browser.close(); }
})();
