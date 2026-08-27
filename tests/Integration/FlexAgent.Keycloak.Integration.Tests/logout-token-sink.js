const http = require("node:http");
const fs = require("node:fs");

http.createServer((request, response) => {
  const chunks = [];
  request.on("data", (chunk) => chunks.push(chunk));
  request.on("end", () => {
    fs.writeFileSync("/tmp/last-form", Buffer.concat(chunks).toString("utf8"));
    response.writeHead(204);
    response.end();
  });
}).listen(8080, () => {
  console.log("listening");
});
