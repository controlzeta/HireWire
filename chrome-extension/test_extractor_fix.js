const fs = require('fs');
const path = require('path');
const { JSDOM } = require('jsdom');

function extractRecruiterNameFixed(doc) {
  if (!doc) return "";

  // Strategy 1: Find container or link near "Conoce al equipo de contratación", "Anunciante del empleo", "Hiring Team", or "Job Poster"
  const inLinks = Array.from(doc.querySelectorAll('a[href*="/in/"]'));
  
  for (const link of inLinks) {
    const href = link.getAttribute('href') || '';
    if (href.includes('/in/me') || href.includes('/in/edit')) continue;

    const container = link.closest('div, section, li, article') || link.parentElement;
    const containerText = container ? container.textContent.toLowerCase() : '';

    const isHiringContext = 
      containerText.includes('equipo de contratación') ||
      containerText.includes('anunciante del empleo') ||
      containerText.includes('técnico de selección') ||
      containerText.includes('hiring team') ||
      containerText.includes('job poster') ||
      containerText.includes('solicitante');

    if (isHiringContext) {
      // 1. Try finding paragraphs (<p>) inside the profile link
      const paragraphs = link.querySelectorAll('p');
      for (const p of paragraphs) {
        // Clone element to remove any svg/icon elements before reading text
        const clone = p.cloneNode(true);
        const svgs = clone.querySelectorAll('svg, [role="img"]');
        svgs.forEach(s => s.remove());
        
        let text = clone.textContent ? clone.textContent.trim() : '';
        text = text.replace(/Verificado|Verified|•.*/gi, '').trim();

        if (text && text.length >= 3 && text.length <= 50) {
          const lower = text.toLowerCase();
          if (!lower.includes('solicitante') && 
              !lower.includes('anunciante') && 
              !lower.includes('equipo') && 
              !lower.includes('técnico') &&
              !lower.includes('partner') &&
              !lower.includes('recruiter') &&
              !lower.includes('ceo') &&
              !lower.includes('manager')) {
            return text;
          }
        }
      }

      // 2. Fallback: Parse child spans inside link
      const spans = link.querySelectorAll('span');
      for (const span of spans) {
        const clone = span.cloneNode(true);
        const svgs = clone.querySelectorAll('svg, [role="img"]');
        svgs.forEach(s => s.remove());

        let text = clone.textContent ? clone.textContent.trim() : '';
        text = text.replace(/Verificado|Verified|•.*/gi, '').trim();

        if (text && text.length >= 3 && text.length <= 50) {
          const lower = text.toLowerCase();
          if (!lower.includes('solicitante') && 
              !lower.includes('anunciante') && 
              !lower.includes('equipo') && 
              !lower.includes('técnico') &&
              !lower.includes('partner') &&
              !lower.includes('recruiter') &&
              !lower.includes('ceo') &&
              !lower.includes('manager')) {
            return text;
          }
        }
      }
    }
  }

  // Strategy 2: Legacy class selectors fallback
  const legacyEl = doc.querySelector(".jobs-poster__name, .hirer-card__name, [class*='jobs-poster'] [class*='name']");
  if (legacyEl) {
    const text = legacyEl.textContent ? legacyEl.textContent.trim() : '';
    if (text && text.length < 50) return text;
  }

  return "";
}

const files = [
  'linkedin.com_2026-08-27_19-15-04.html',
  'linkedin.com_2026-08-27_19-27-26.html'
];

files.forEach(file => {
  const filePath = path.join(__dirname, '../html', file);
  if (!fs.existsSync(filePath)) return;

  const html = fs.readFileSync(filePath, 'utf8');
  const dom = new JSDOM(html);
  const recruiter = extractRecruiterNameFixed(dom.window.document);

  console.log(`File: ${file} => Extracted Recruiter: "${recruiter}"`);
});
