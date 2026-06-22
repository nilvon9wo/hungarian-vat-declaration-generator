/** @jsxImportSource react */
import type { VatDeclarationResult } from '../types/api';
import { ResultsHeader } from './ResultsHeader';
import { VatCategoriesTable } from './VatCategoriesTable';
import { GrandTotalsTable } from './GrandTotalsTable';

interface VatResultsProps {
  result: VatDeclarationResult;
  isLoading: boolean;
  onDownloadPdf: () => void;
}

export function VatResults({ result, isLoading, onDownloadPdf }: VatResultsProps) {
  return (
    <section className="vat-app__results-section results">
      <ResultsHeader isLoading={isLoading} onDownloadPdf={onDownloadPdf} />

      <div className="results__summary summary-info">
        <p className="summary-info__text">
          Total Invoices Processed: <strong>{result.totalInvoiceCount}</strong>
        </p>
      </div>

      <VatCategoriesTable summaries={result.summariesByVatRate} />
      <GrandTotalsTable
        grandTotalNet={result.grandTotalNet}
        grandTotalVat={result.grandTotalVat}
        grandTotalGross={result.grandTotalGross}
      />
    </section>
  );
}
