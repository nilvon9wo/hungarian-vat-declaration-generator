/** @jsxImportSource react */

interface ResultsHeaderProps {
  isLoading: boolean;
  onDownloadPdf: () => void;
}

export function ResultsHeader({ isLoading, onDownloadPdf }: ResultsHeaderProps) {
  return (
    <div className="results__header">
      <h2 className="results__title">VAT Declaration Results</h2>
      <button onClick={onDownloadPdf} disabled={isLoading} className="results__download-button">
        Download PDF Report
      </button>
    </div>
  );
}
