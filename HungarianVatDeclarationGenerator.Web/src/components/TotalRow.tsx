/** @jsxImportSource react */
import { formatCurrency } from '../utils/currency';

interface TotalRowProps {
  label: string;
  value: number;
}

export function TotalRow({ label, value }: TotalRowProps) {
  return (
    <tr className="totals-table__row">
      <td className="totals-table__label">{label}</td>
      <td className="totals-table__value">{formatCurrency(value)}</td>
    </tr>
  );
}
