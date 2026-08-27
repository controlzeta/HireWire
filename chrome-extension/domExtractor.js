// Isomorphic module for extracting job posting details from the DOM

function getText(el) {
  if (!el) return "";
  return (el.innerText || el.textContent || "").trim();
}

function extractJobTitle(doc) {
  if (!doc) return "";
  
  // Specific classes first
  const specificTitleEl = doc.querySelector(
    ".job-details-jobs-unified-top-card__job-title, .jobs-unified-top-card__job-title, .job-title, .jobs-title"
  );
  if (specificTitleEl) {
    const text = getText(specificTitleEl);
    if (text) return text;
  }

  // Active search result item title in list
  const activeTitleEl = doc.querySelector(
    "[class*='--active'] .job-card-list__title, [class*='--selected'] .job-card-list__title, [class*='--active'] a[href*='/jobs/view/'], [class*='--selected'] a[href*='/jobs/view/']"
  );
  if (activeTitleEl) {
    const text = getText(activeTitleEl);
    if (text) return text;
  }

  return "";
}

function extractCompanyName(doc) {
  if (!doc) return "";
  
  // 1. Try standard company links
  const compEl = doc.querySelector(
    ".job-details-jobs-unified-top-card__company-name a, .jobs-unified-top-card__company-name a, .jobs-unified-top-card__primary-description a"
  );
  if (compEl) {
    return getText(compEl).replace(/[\n\r]/g, "").trim();
  }

  // 2. Try links that point to company profile with aria-labels or specific text (handles SDUI)
  const companyEl = doc.querySelector(
    "a[href*='/company/'], [aria-label*='Empresa,'], [aria-label*='Company,']"
  );
  if (companyEl) {
    let label = companyEl.getAttribute("aria-label") || getText(companyEl);
    if (label) {
      label = label.replace(/^(Empresa|Company|Logotipo de empresa para),\s*/i, "").trim();
      if (label && !label.toLowerCase().includes("ir a la página")) {
        return label;
      }
    }
  }
  
  // 3. Try active search result item company name in list
  const activeCompanyEl = doc.querySelector(
    "[class*='--active'] .job-card-container__company-name, [class*='--active'] a[href*='/company/'], [class*='--selected'] a[href*='/company/']"
  );
  if (activeCompanyEl) {
    const text = getText(activeCompanyEl);
    if (text) return text;
  }

  const compBackup = doc.querySelector(
    ".job-details-jobs-unified-top-card__company-name, .jobs-unified-top-card__company-name, .company-name, [class*='company']"
  );
  if (compBackup) {
    const text = getText(compBackup).replace(/[\n\r]/g, "").trim();
    if (text && !text.toLowerCase().includes("ir a la página")) {
      return text;
    }
  }
  return "";
}

function extractJobDescription(doc) {
  if (!doc) return "";
  
  // Try standard selectors first (including test IDs for expandable text boxes)
  const descEl = doc.querySelector(
    "#job-details, [data-testid='expandable-text-box'], .jobs-description__content, .jobs-box__html-content, .job-description, .content"
  );
  if (descEl) {
    const text = getText(descEl);
    if (text.length > 50) return text;
  }

  // Fallback to "Acerca del empleo" / "About the job" siblings (for saved HTML files)
  let targetH2 = null;
  doc.querySelectorAll("h2").forEach(h2 => {
    const text = h2.textContent.toLowerCase();
    if (text.includes("acerca del empleo") || text.includes("about the job")) {
      targetH2 = h2;
    }
  });

  if (targetH2 && targetH2.parentElement) {
    let parentSib = targetH2.parentElement.nextElementSibling;
    let paragraphs = [];
    while (parentSib) {
      const tagName = parentSib.tagName.toLowerCase();
      if (tagName === "h2" || tagName === "section" || parentSib.querySelector("h2")) {
        break;
      }
      
      const text = getText(parentSib);
      if (text) {
        paragraphs.push(text);
      }
      parentSib = parentSib.nextElementSibling;
    }
    
    if (paragraphs.length > 0) {
      return paragraphs.join("\n\n");
    }
  }

  // Backup fallback
  const mainText = doc.querySelector("main, body");
  if (mainText) {
    return getText(mainText).substring(0, 4000).trim();
  }
  return "";
}

function extractSalaryRange(doc) {
  if (!doc) return "";
  const salaryEl = doc.querySelector(
    ".job-details-jobs-unified-top-card__job-insight, .jobs-unified-top-card__job-insight, [class*='salary'], [class*='compensation']"
  );
  return getText(salaryEl);
}

function extractRecruiterName(doc) {
  if (!doc) return "";

  // 1. LinkedIn specific hiring manager / recruiter card selectors
  const recruiterEl = doc.querySelector(
    ".jobs-poster__name, .hirer-card__name, [class*='jobs-poster'] [class*='name'], [class*='hiring-team'] [class*='name'], [class*='hirer-card'] a[href*='/in/'], [class*='hiring-team'] a[href*='/in/']"
  );
  if (recruiterEl) {
    const text = getText(recruiterEl);
    if (text && text.length < 80) return text;
  }

  // 2. Search links inside hiring team containers or with "Hiring Manager" context
  const posterLinks = doc.querySelectorAll("a[href*='/in/'], [class*='poster'], [class*='hirer']");
  for (const el of posterLinks) {
    const parentText = el.parentElement ? el.parentElement.textContent.toLowerCase() : "";
    if (parentText.includes("solicitante del empleo") || parentText.includes("hiring manager") || parentText.includes("publicado por")) {
      const text = getText(el);
      if (text && !text.toLowerCase().includes("ver perfil") && text.length < 80) {
        return text;
      }
    }
  }

  return "";
}

function extractPlatform(url) {
  if (!url) return "LinkedIn";
  if (url.includes("linkedin.com")) return "LinkedIn";
  if (url.includes("getmanfred.com") || url.includes("manfred")) return "Manfred";
  return "LinkedIn";
}

function extractJobDetails(doc, url) {
  if (!doc) return null;

  const platform = extractPlatform(url);
  
  // 1. Extraer los datos del DOM (más específico y confiable para páginas de búsqueda)
  let jobTitle = extractJobTitle(doc);
  let companyName = extractCompanyName(doc);

  // 2. Fallback a la interpretación del título de la pestaña de Chrome
  if (platform === "LinkedIn") {
    if (!jobTitle || !companyName || companyName === "Empresa Desconocida") {
      const pageTitle = doc.title || "";
      if (pageTitle.includes("|") && pageTitle.includes("LinkedIn")) {
        const parts = pageTitle.split("|").map(p => p.trim());
        if (parts.length >= 2) {
          if (!jobTitle) jobTitle = parts[0];
          if (!companyName || companyName === "Empresa Desconocida") {
            // Evitar usar "LinkedIn" o "Search" como el nombre de la empresa si es posible
            if (parts[1] !== "LinkedIn" && parts[1] !== "Search") {
              companyName = parts[1];
            }
          }
        }
      }
    }
  }

  // 3. Fallbacks finales de seguridad
  if (!jobTitle) {
    jobTitle = doc.title ? doc.title.split("|")[0].trim() : "Vacante Desconocida";
  }
  if (!companyName || companyName === "Empresa Desconocida") {
    companyName = "Empresa Desconocida";
  }

  // 4. Limpieza del URL para guardar enlaces limpios de visualización directa
  let cleanUrl = url;
  if (url.includes("linkedin.com") && url.includes("currentJobId=")) {
    const match = url.match(/currentJobId=(\d+)/);
    if (match && match[1]) {
      cleanUrl = `https://www.linkedin.com/jobs/view/${match[1]}/`;
    }
  }

  const rawDescription = extractJobDescription(doc) || "Descripción no extraíble automáticamente.";
  const salaryRange = extractSalaryRange(doc) || null;
  const recruiterName = extractRecruiterName(doc) || null;

  return {
    companyName,
    industry: null,
    companyContext: null,
    jobTitle,
    url: cleanUrl,
    platform,
    rawDescription,
    salaryRange,
    recruiterName
  };
}

// Export for Node environments (Jest)
if (typeof module !== "undefined" && module.exports) {
  module.exports = {
    extractJobTitle,
    extractCompanyName,
    extractJobDescription,
    extractSalaryRange,
    extractRecruiterName,
    extractPlatform,
    extractJobDetails
  };
}
