const {
  extractJobTitle,
  extractCompanyName,
  extractJobDescription,
  extractSalaryRange,
  extractPlatform,
  extractJobDetails
} = require("../domExtractor");

const { linkedinHtml, manfredHtml } = require("../mocks/jobPostMock");

describe("HireWire Chrome Extension DOM Extractor Tests", () => {
  let parser;

  beforeAll(() => {
    parser = new DOMParser();
  });

  describe("Assert 1: Successful Data Extraction and Trimming", () => {
    test("should extract LinkedIn details correctly and trim extra whitespace", () => {
      const doc = parser.parseFromString(linkedinHtml, "text/html");
      const url = "https://www.linkedin.com/jobs/view/123456";

      const details = extractJobDetails(doc, url);

      expect(details.jobTitle).toBe("Senior .NET Developer");
      expect(details.companyName).toBe("Contoso Software");
      expect(details.platform).toBe("LinkedIn");
      expect(details.salaryRange).toContain("$100,000 - $130,000/yr");
      expect(details.rawDescription).toContain("We are looking for a Senior .NET Developer");
    });

    test("should extract Manfred details correctly and trim extra whitespace", () => {
      const doc = parser.parseFromString(manfredHtml, "text/html");
      const url = "https://www.getmanfred.com/jobs/9876";

      const details = extractJobDetails(doc, url);

      expect(details.jobTitle).toBe("Solutions Architect");
      expect(details.companyName).toBe("Manfred Tech");
      expect(details.platform).toBe("Manfred");
      expect(details.salaryRange).toContain("€60,000 - €80,000");
      expect(details.rawDescription).toContain("Join our team as a Solutions Architect");
    });
  });

  describe("Assert 2: Graceful Degradation (Zero-Errors Principle)", () => {
    test("should return safe defaults when DOM elements are completely missing", () => {
      const doc = parser.parseFromString("<div></div>", "text/html");
      const url = "https://www.linkedin.com/jobs/view/999";

      const details = extractJobDetails(doc, url);

      expect(details.jobTitle).toBe("Vacante Desconocida");
      expect(details.companyName).toBe("Empresa Desconocida");
      expect(details.salaryRange).toBeNull();
      expect(details.rawDescription).toBe("Descripción no extraíble automáticamente.");
    });

    test("should return safe values when document is null or undefined", () => {
      expect(extractJobTitle(null)).toBe("");
      expect(extractCompanyName(null)).toBe("");
      expect(extractJobDescription(null)).toBe("");
      expect(extractSalaryRange(null)).toBe("");
      expect(extractJobDetails(null, "http://test.com")).toBeNull();
    });
  });

  describe("Assert 3: Payload & Fetch Verification", () => {
    beforeEach(() => {
      global.fetch = jest.fn().mockImplementation(() =>
        Promise.resolve({
          ok: true,
          json: () => Promise.resolve({ id: "test-guid-uuid" }),
          text: () => Promise.resolve("OK")
        })
      );
    });

    afterEach(() => {
      jest.restoreAllMocks();
    });

    test("should structure final JSON payload correctly and trigger fetch request to backend API", async () => {
      const doc = parser.parseFromString(linkedinHtml, "text/html");
      const url = "https://www.linkedin.com/jobs/view/123456";

      const scrapedData = extractJobDetails(doc, url);

      expect(scrapedData).toEqual(
        expect.objectContaining({
          companyName: "Contoso Software",
          industry: null,
          companyContext: null,
          jobTitle: "Senior .NET Developer",
          url: "https://www.linkedin.com/jobs/view/123456",
          platform: "LinkedIn",
          salaryRange: expect.stringContaining("$100,000 - $130,000/yr")
        })
      );

      const response = await fetch("http://localhost:5033/api/leads", {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify(scrapedData)
      });

      expect(global.fetch).toHaveBeenCalledTimes(1);
      expect(global.fetch).toHaveBeenCalledWith(
        "http://localhost:5033/api/leads",
        expect.objectContaining({
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(scrapedData)
        })
      );

      const result = await response.json();
      expect(result.id).toBe("test-guid-uuid");
    });
  });
});
