/** @jsxImportSource react */
import { TotalRow } from './TotalRow';

interface GrandTotalsTableProps {
  grandTotalNet: number;
  grandTotalVat: number;
  grandTotalGross: number;
}

export function GrandTotalsTable({ grandTotalNet, grandTotalVat, grandTotalGross }: GrandTotalsTableProps) {
  return (
    <div className="results__grand-totals grand-totals">
      <h3 className="grand-totals__title">Grand Totals</h3>
      <table className="grand-totals__table totals-table">
        <tbody className="totals-table__body">
          <TotalRow label="Total Net Amount:" value={grandTotalNet} />
          <TotalRow label="Total VAT Amount:" value={grandTotalVat} />
          <TotalRow label="Total Gross Amount:" value={grandTotalGross} />
        </tbody>
      </table>
    </div>
  );
}
