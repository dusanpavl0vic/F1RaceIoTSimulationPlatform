const zlib = require("zlib");

const base64 =
  "tZa9blsxDIXfRbNvIFL8k/fMLdAMbYoOQZHBKJIUiTsFfvdKV+Jtl0j24MWwAR9QOjz8xPfw+eXtcDy8PIf99/dwd3h6fDs+PP0O+4ARYYl5AbyDtEfbJ72xFJlB7sMu3D4fXw+Pb2H/HlL9+HJ8OP4pP8On57vXh5+/yl++hr0yp134FvbAwHEX7su3zPm0C/SxiFG6iEiwi0SLiD8WLZByXlVLztpEprGIZCTCyK0UMnSRQRHlj0UgQKumSKJrrGggjipFbipL/UomUkUwEmU/XfG9G5GqETCwb8lRVxUpuBG5Wg4DJ4BzO16xkbwUpaqyQSlRWFVK7nmGKkIcmg6tUxAtu4G1U2noBWpTpewGarUijeKHqVfiqMmTVPtLAwMJo/T8rZlqKqoqHThI3L1I+f/z8TC03Bpsih50rIVkcKkFyKwlHSl7agWLTAfnW7SHSSJYrxVrAm3U4NytWDihh4mqKg8mRDXF3mBNngu00+m0mzHGbiJHBsMLGCOkzhgRZ4xNGENkzhiN5zPGmoeLwRZCmDEGbCu1xR0njOEOM0zktptehzHJGYOVa40xMmOM5dbhEkCnRYwzxqj0UhTBG0U0ZYz0uZKtFNC1GFPfm6JiM2+vzRiToscvZr+VxBljUt5CARuZeMqY3iwWn3uVKWPEOmPULmEMdMakLeqSpozRhgtBh2B5jKaMyb0Uo1PwDMZwH0WIQvFCxqBGMtILGKMkzhjzKWGdMEbwH2PkfMZI32OKid5knDKmP8Qg5Mm1NGOMtTULC25dI1dhjHTzMCmcvcaYtuMxbAMSYYaYTOCI2dK+ZmmMGG5jr4oeJeBrIYZabtXEu5uniOnDWNIH5yOG87ZG+9ud116NEQMttGkLhfIUMT1JJXPbc89zxCh1xEDyRXpds8aI6c+B0LYyRZshpiwizpjSLWeMzhhDPvUl9B5c1MKYH6e/";

try {
  const buffer = Buffer.from(base64, "base64");

  const result = zlib.inflateRawSync(buffer);
  const text = result.toString("utf-8");

  console.log("Dekodirano:\n");
  console.log(text);

  const json = JSON.parse(text);

  console.log("\nPrvi timestamp:");
  console.log(json.Position?.[0]?.Timestamp);

  console.log("\nPrvi auto iz prvog zapisa:");
  const firstEntries = json.Position?.[0]?.Entries || {};
  const firstCarNumber = Object.keys(firstEntries)[0];
  console.log(firstCarNumber, firstEntries[firstCarNumber]);
} catch (err) {
  console.error("Greška:", err.message);
}
