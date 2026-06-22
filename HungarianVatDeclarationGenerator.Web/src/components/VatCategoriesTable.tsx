/** @jsxImportSource react */
import type { VatSummary } from '../types/api';
import { formatCurrency } from '../utils/currency';

interface VatCategoriesTableProps {
  summaries: VatSummary[];
}

export function VatCategoriesTable({ summaries }: VatCategoriesTableProps) {
  return (
    <>
      <h3 className="results__subtitle">VAT Categories</h3>
      <table className="results__table vat-table">
        <thead className="vat-table__head">
          <tr className="vat-table__row">
            <th className="vat-table__header">VAT Rate</th>
            <th className="vat-table__header vat-table__header--number">Net Amount</th>
            <th className="vat-table__header vat-table__header--number">VAT Amount</th>
            <th className="vat-table__header vat-table__header--number">Gross Amount</th>
            <th className="vat-table__header vat-table__header--number">Invoice Count</th>
          </tr>
        </thead>
        <tbody className="vat-table__body">
          {summaries.map((summary) => (
            <tr key={summary.vatRate} className="vat-table__row">
              <td className="vat-table__cell">{summary.vatRate}%</td>
              <td className="vat-table__cell vat-table__cell--number">{formatCurrency(summary.totalNetAmount)}</td>
              <td className="vat-table__cell vat-table__cell--number">{formatCurrency(summary.totalVatAmount)}</td>
              <td className="vat-table__cell vat-table__cell--number">{formatCurrency(summary.totalGrossAmount)}</td>
              <td className="vat-table__cell vat-table__cell--number">{summary.invoiceCount}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </>
  );
}
