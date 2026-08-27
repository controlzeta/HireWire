// Mock HTML structures for LinkedIn and Manfred job postings (moved from __mocks__ to avoid Chrome restriction)

const linkedinHtml = `
<div class="jobs-unified-top-card">
  <h1 class="jobs-unified-top-card__job-title">  Senior .NET Developer  </h1>
  <div class="jobs-unified-top-card__company-name">
    <a href="#">  Contoso Software  </a>
  </div>
  <div class="jobs-unified-top-card__job-insight">
    <span> $100,000 - $130,000/yr • Full-time </span>
  </div>
  <div id="job-details">
    <h3>About the job</h3>
    <p> We are looking for a Senior .NET Developer to build cloud native APIs. </p>
  </div>
</div>
`;

const manfredHtml = `
<main>
  <h1 class="job-title"> Solutions Architect </h1>
  <div class="company-name">
    <a href="#"> Manfred Tech </a>
  </div>
  <div class="salary">
    €60,000 - €80,000
  </div>
  <div class="job-description">
    <p> Join our team as a Solutions Architect to design Azure infrastructures. </p>
  </div>
</main>
`;

module.exports = {
  linkedinHtml,
  manfredHtml
};
