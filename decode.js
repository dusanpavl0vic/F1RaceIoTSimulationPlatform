const zlib = require("zlib");

const base64 =
"7ZYxb8IwEIX/y80p8tln39l751YqQ0vVAVUMUQVUkE4o/70JJmnS4WBg9BIpUj69u+fnp5zgeX+sm3q/g/R+gmW93Ryb9fYbElhj8cHEB7RLtMlJwrAQNhSJVlDB46451JsjpBO4/vHSrJuf7hWedsvD+vOr++QVUkczV/AGiaxUsIKEwtxWQCpDdGZYRiZ2jNd1fNZxNNUJKiOZcdYOTOiYqOtg1uHZPmg0yPhsguAABd9DeNt0cTod6taxzRDRDNJ9YJd36sec7CQqFMIZ8t5PIWtVCOMZQiPDUmI6yulOjBmyUymnBs8ZukiNUO856fYFyUvNU0SsxzV7zjQelPRx1fM62OfiLK/6ZbqM9y/krI8Xc46smeVI9NOVHAnnw+xqXLkb9pKj8Dde21bXioUXJhoKWHql9ErpldIr9+sVh4Ysh1IspVhKsZRiuV+xeGdcpPLHUoqlFEsplluL5aP9BQ=="

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
