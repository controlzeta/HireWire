// Invokes the globally loaded extraction logic from domExtractor.js in target page context
(() => {
  return extractJobDetails(document, window.location.href);
})();
