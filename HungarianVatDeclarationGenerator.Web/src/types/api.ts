export interface VatSummary {
  vatRate: number;
  totalNetAmount: number;
  totalVatAmount: number;
  totalGrossAmount: number;
  invoiceCount: number;
}

export interface VatDeclarationResult {
  summariesByVatRate: VatSummary[];
  grandTotalNet: number;
  grandTotalVat: number;
  grandTotalGross: number;
  totalInvoiceCount: number;
}

export interface ApiError {
  error: string;
}

export interface ClientConfig {
  maxFileSizeBytes: number;
  allowedExtensions: string[];
  requestTimeoutSeconds: number;
}
