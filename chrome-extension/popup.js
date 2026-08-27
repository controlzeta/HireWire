// JavaScript controller for Chrome Extension popup
console.log("🚀 [HireWire Extension] Popup script loaded successfully v1.0.1!");

function injectToastIntoPage(tabId, title, message, isError) {
  console.log(`🚀 [HireWire Extension] Injecting toast: "${title}" - "${message}" (isError: ${isError})`);
  try {
    chrome.scripting.executeScript({
      target: { tabId: tabId },
      func: (title, message, isError) => {
        // Find or create toast container
        let container = document.getElementById("hirewire-toast-container");
        if (!container) {
          container = document.createElement("div");
          container.id = "hirewire-toast-container";
          container.style.position = "fixed";
          container.style.top = "24px";
          container.style.right = "24px";
          container.style.zIndex = "9999999";
          container.style.display = "flex";
          container.style.flexDirection = "column";
          container.style.gap = "12px";
          container.style.pointerEvents = "none";
          document.body.appendChild(container);
        }

        // Create toast card
        const toast = document.createElement("div");
        toast.style.pointerEvents = "auto";
        toast.style.minWidth = "300px";
        toast.style.maxWidth = "400px";
        toast.style.backgroundColor = isError ? "#fff5f5" : "#f4fcf7";
        toast.style.border = `1px solid ${isError ? "#ffc9c9" : "#a3e2bb"}`;
        toast.style.borderRadius = "8px";
        toast.style.padding = "16px";
        toast.style.boxShadow = "0 8px 24px rgba(0,0,0,0.12)";
        toast.style.fontFamily = "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif";
        toast.style.color = isError ? "#fa5252" : "#2b8a3e";
        toast.style.opacity = "0";
        toast.style.transform = "translateY(-20px)";
        toast.style.transition = "all 0.3s ease";

        toast.innerHTML = `
          <div style="display: flex; align-items: flex-start; gap: 12px;">
            <div style="font-size: 20px; line-height: 1;">${isError ? "❌" : "✅"}</div>
            <div style="flex: 1;">
              <div style="font-weight: 700; font-size: 14px; margin-bottom: 4px; color: ${isError ? "#c92a2a" : "#2b8a3e"}">${title}</div>
              <div style="font-size: 13px; color: #495057; line-height: 1.4;">${message}</div>
            </div>
          </div>
        `;

        container.appendChild(toast);

        // Trigger animation
        requestAnimationFrame(() => {
          toast.style.opacity = "1";
          toast.style.transform = "translateY(0)";
        });

        // Remove toast after 4 seconds
        setTimeout(() => {
          toast.style.opacity = "0";
          toast.style.transform = "translateY(-10px)";
          setTimeout(() => toast.remove(), 300);
        }, 4000);
      },
      args: [title, message, isError]
    });
  } catch (e) {
    console.warn("Failed to execute toast injection script:", e);
  }
}

function showNotification(tabId, title, message, isError = false) {
  console.log(`[Notification Request] ${title}: ${message}`);
  
  // 1. Inyectar notificación directamente en la página web activa
  if (tabId && typeof chrome !== "undefined" && chrome.scripting) {
    injectToastIntoPage(tabId, title, message, isError);
  }

  // 2. Intentar notificación del sistema operativo a través de Chrome
  if (typeof chrome === "undefined" || !chrome.notifications || !chrome.runtime || !chrome.runtime.getURL) {
    console.warn("Chrome System Notifications are not available in this context.");
    return;
  }

  try {
    const iconUrl = chrome.runtime.getURL("icon.png");
    chrome.notifications.create("", {
      type: "basic",
      iconUrl: iconUrl,
      title: title,
      message: message,
      priority: 2
    }, (id) => {
      if (chrome.runtime.lastError) {
        console.error("Chrome Notifications API error:", chrome.runtime.lastError.message);
      } else {
        console.log("Notification created successfully. ID:", id);
      }
    });
  } catch (e) {
    console.error("Failed to show system notification:", e);
  }
}

// Load saved endpoint on popup open (Defensively wrapped to prevent script load failures)
document.addEventListener("DOMContentLoaded", () => {
  console.log("🚀 [HireWire Extension] DOMContentLoaded initialized");
  try {
    const savedEndpoint = localStorage.getItem("hirewire_endpoint");
    const endpointInput = document.getElementById("endpointInput");
    if (savedEndpoint && endpointInput) {
      endpointInput.value = savedEndpoint;
    }
  } catch (err) {
    console.warn("Failed to initialize saved endpoint:", err);
  }
});

// Click Handler Setup (Defensively wrapped to ensure listener registration)
const extractBtn = document.getElementById("extractBtn");
if (extractBtn) {
  extractBtn.addEventListener("click", async () => {
    console.log("🚀 [HireWire Extension] Extract button clicked!");
    const btnText = document.getElementById("btnText");
    const btnSpinner = document.getElementById("btnSpinner");
    const statusDiv = document.getElementById("statusDiv");
    const endpointInput = document.getElementById("endpointInput");

    if (statusDiv) {
      statusDiv.className = "status";
      statusDiv.innerText = "Extrayendo datos de la pestaña activa...";
    }

    // Activar estado visual de carga (loader) de forma defensiva
    extractBtn.disabled = true;
    if (btnText) {
      btnText.innerText = "Procesando...";
    } else {
      extractBtn.innerText = "Procesando...";
    }
    if (btnSpinner) {
      btnSpinner.style.display = "inline-block";
    }

    function resetButtonState() {
      extractBtn.disabled = false;
      if (btnText) {
        btnText.innerText = "Extraer Vacante";
      } else {
        extractBtn.innerText = "Extraer Vacante";
      }
      if (btnSpinner) {
        btnSpinner.style.display = "none";
      }
    }

    let tabId = null;
    let tabUrl = "";
    try {
      let [tab] = await chrome.tabs.query({ active: true, lastFocusedWindow: true });
      if (!tab) {
        [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
      }
      if (tab) {
        tabId = tab.id;
        tabUrl = tab.url || "";
        console.log(`🚀 [HireWire Extension] Active tab found: ID=${tabId}, URL=${tabUrl}`);
      }
    } catch (e) {
      console.warn("Failed to get active tab:", e);
    }

    showNotification(tabId, "HireWire Extractor", "Iniciando extracción de la vacante...", false);

    try {
      if (!tabId) {
        throw new Error("No se pudo identificar una pestaña activa.");
      }

      // Inject and execute domExtractor.js and content.js in the target tab using promise-based syntax
      let results;
      try {
        results = await chrome.scripting.executeScript({
          target: { tabId: tabId },
          files: ["domExtractor.js", "content.js"]
        });
      } catch (injectErr) {
        if (statusDiv) {
          statusDiv.className = "status error";
          statusDiv.innerText = "Error de inyección: " + injectErr.message;
        }
        showNotification(tabId, "Error de Extracción", "No se pudo inyectar el script en la pestaña: " + injectErr.message, true);
        resetButtonState();
        return;
      }

      if (!results || results.length === 0 || !results[0].result) {
        if (statusDiv) {
          statusDiv.className = "status error";
          statusDiv.innerText = "No se pudieron recuperar datos de la vacante.";
        }
        showNotification(tabId, "Error de Extracción", "No se encontraron datos legibles en la página.", true);
        resetButtonState();
        return;
      }

      const scrapedData = results[0].result;
      if (statusDiv) {
        statusDiv.innerText = `Vacante: "${scrapedData.jobTitle}" @ ${scrapedData.companyName}. Enviando a HireWire...`;
      }

      // Enviar POST a la API especificada por el usuario o usar la de fallback
      const primaryUrl = (endpointInput ? endpointInput.value.trim() : "") || "https://localhost:7165/api/leads";
      localStorage.setItem("hirewire_endpoint", primaryUrl);

      try {
        const response = await fetch(primaryUrl, {
          method: "POST",
          headers: {
            "Content-Type": "application/json"
          },
          body: JSON.stringify(scrapedData)
        });

        if (!response.ok) {
          const errText = await response.text();
          throw new Error(`HTTP ${response.status}: ${errText}`);
        }

        const data = await response.json();
        if (statusDiv) {
          statusDiv.className = "status success";
          statusDiv.innerText = `¡Guardado! ID: ${data.id || data.Id}`;
        }
        showNotification(tabId, "¡Éxito!", `Vacante "${scrapedData.jobTitle}" guardada con éxito en HireWire.`, false);
        resetButtonState();
      } catch (err) {
        console.warn("API request failed, checking fallback...", err);
        
        // Si el endpoint primario es el default HTTPS, intentamos el fallback HTTP por defecto
        if (primaryUrl === "https://localhost:7165/api/leads") {
          try {
            const httpUrl = "http://localhost:5033/api/leads";
            const httpResponse = await fetch(httpUrl, {
              method: "POST",
              headers: {
                "Content-Type": "application/json"
              },
              body: JSON.stringify(scrapedData)
            });
            
            if (!httpResponse.ok) {
              const errText = await httpResponse.text();
              throw new Error(`HTTP ${httpResponse.status}: ${errText}`);
            }

            const data = await httpResponse.json();
            if (statusDiv) {
              statusDiv.className = "status success";
              statusDiv.innerText = `¡Guardado! ID: ${data.id || data.Id}`;
            }
            showNotification(tabId, "¡Éxito!", `Vacante "${scrapedData.jobTitle}" guardada con éxito en HireWire.`, false);
            resetButtonState();
            return;
          } catch (httpErr) {
            console.error("HTTP fallback failed:", httpErr);
          }
        }

        if (statusDiv) {
          statusDiv.className = "status error";
          statusDiv.innerText = "Fallo de conexión con API: " + err.message;
        }
        showNotification(tabId, "Error de Conexión", "No se pudo comunicar con el servidor local de HireWire: " + err.message, true);
        resetButtonState();
      }

    } catch (error) {
      if (statusDiv) {
        statusDiv.className = "status error";
        statusDiv.innerText = error.message;
      }
      showNotification(tabId, "Error", error.message, true);
      resetButtonState();
    }
  });
} else {
  console.warn("Element '#extractBtn' not found in HTML.");
}
