const fs = require('fs');
const zlib = require('zlib');
const S = 256;
const px = Buffer.alloc(S * S * 4);
function set(x, y, r, g, b) { const i = (y * S + x) * 4; px[i] = r; px[i + 1] = g; px[i + 2] = b; px[i + 3] = 255; }
for (let y = 0; y < S; y++) for (let x = 0; x < S; x++) set(x, y, 30, 30, 34);
for (let y = 200; y < 232; y++) for (let x = 24; x < S - 24; x++) x < S / 2 ? set(x, y, 30, 63, 168) : set(x, y, 168, 30, 30);
for (let y = 60; y < 180; y++) for (let x = 48; x < 208; x++) {
  const base = y >= 130;
  const h = y - 60;
  const half = Math.max(6, 30 - Math.floor(h / 3));
  const spike = Math.abs(x - 80) <= half || Math.abs(x - 128) <= half + 8 || Math.abs(x - 176) <= half;
  if (base || spike) set(x, y, 245, 197, 66);
}
function crc32(buf) { let c, crc = 0xffffffff; for (let n = 0; n < buf.length; n++) { c = (crc ^ buf[n]) & 0xff; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; crc = (crc >>> 8) ^ c; } return (crc ^ 0xffffffff) >>> 0; }
function chunk(type, data) { const len = Buffer.alloc(4); len.writeUInt32BE(data.length); const td = Buffer.concat([Buffer.from(type), data]); const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(td)); return Buffer.concat([len, td, crc]); }
const raw = Buffer.alloc((S * 4 + 1) * S);
for (let y = 0; y < S; y++) { raw[y * (S * 4 + 1)] = 0; px.copy(raw, y * (S * 4 + 1) + 1, y * S * 4, (y + 1) * S * 4); }
const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(S, 0); ihdr.writeUInt32BE(S, 4); ihdr[8] = 8; ihdr[9] = 6; ihdr[10] = 0; ihdr[11] = 0; ihdr[12] = 0;
const png = Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ihdr), chunk('IDAT', zlib.deflateSync(raw)), chunk('IEND', Buffer.alloc(0))]);
fs.writeFileSync(process.argv[2] || 'thunderstore/icon.png', png);
console.log('icon written', png.length, 'bytes');
