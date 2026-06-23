import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import App from './App';
import * as api from './services/api';
import type { ClientConfig } from './types/api';

vi.mock('./services/api');

// Hungarian VAT rate as defined by Hungarian tax law
const STANDARD_HUNGARIAN_VAT_RATE: number = 27;

// Test file and content constants
const SAMPLE_CSV_FILENAME: string = 'test.csv';
const TEXT_CSV_MIME_TYPE: string = 'text/csv';
const APPLICATION_PDF_MIME_TYPE: string = 'application/pdf';

const SAMPLE_ERROR_MESSAGE: string = 'Invalid CSV format';
const PDF_CONTENT: string = 'pdf content';
const BLOB_MOCK_URL: string = 'blob:mock-url';
const TEST_FILE_CONTENT: string = 'test';

const UI_TITLE_TEXT: string = 'Hungarian VAT Declaration Generator';
const UI_FILE_INPUT_LABEL: string = 'Upload CSV File:';
const UI_SUBMIT_BUTTON_TEXT: RegExp = /Generate VAT Declaration/i;
const UI_RESULTS_TITLE_TEXT: string = 'VAT Declaration Results';
const UI_TOTAL_INVOICES_TEXT: RegExp = /Total Invoices Processed:/i;
const UI_VAT_CATEGORIES_TEXT: RegExp = /VAT Categories/i;
const UI_DOWNLOAD_PDF_TEXT: string = 'Download PDF Report';
const UI_PROCESSING_TEXT: string = 'Processing...';
const UI_CURRENCY_SUFFIX: RegExp = /Ft/i;

const MOCK_CONFIG: ClientConfig = {
  maxFileSizeBytes: 5242880,
  allowedExtensions: ['.csv'],
  requestTimeoutSeconds: 30
};

describe('App Component', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(api.fetchConfig).mockResolvedValue(MOCK_CONFIG);
  });

  it('should render the upload form', async () => {
    // Act
    render(<App />);

    // Assert
    await waitFor(() => {
      expect(screen.getByText(UI_TITLE_TEXT)).toBeInTheDocument();
    });
    expect(screen.getByLabelText(UI_FILE_INPUT_LABEL)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: UI_SUBMIT_BUTTON_TEXT })).toBeInTheDocument();
  });

  it('should enable submit button when file is selected', async () => {
    // Arrange
    const user = userEvent.setup();
    render(<App />);
    const submitButton: HTMLElement = getSubmitButton();

    // Sanity check - button should be disabled initially
    expect(submitButton).toBeDisabled();

    // Act
    await uploadFile(user, createTestCsvFile());

    // Assert
    expect(submitButton).not.toBeDisabled();
  });

  it('should display error when form is submitted without file', async () => {
    // Act
    render(<App />);
    const submitButton: HTMLElement = getSubmitButton();

    // Assert
    expect(submitButton).toBeDisabled();
  });

  it('should display results after successful upload', async () => {
    // Arrange
    const user = userEvent.setup();
    vi.mocked(api.uploadCsvFile).mockResolvedValueOnce(createMockVatResult());
    render(<App />);
    await uploadFile(user, createTestCsvFile());

    // Act
    await clickSubmitButton(user);

    // Assert
    await waitFor(() => {
      expect(screen.getByText(UI_RESULTS_TITLE_TEXT)).toBeInTheDocument();
    });
    expect(screen.getByText(UI_TOTAL_INVOICES_TEXT)).toBeInTheDocument();
    expect(screen.getByText(`${STANDARD_HUNGARIAN_VAT_RATE}%`)).toBeInTheDocument();
    expect(screen.getByText(UI_VAT_CATEGORIES_TEXT)).toBeInTheDocument();
  });

  it('should display error message on upload failure', async () => {
    // Arrange
    const user = userEvent.setup();
    vi.mocked(api.uploadCsvFile).mockResolvedValueOnce(
      createMockErrorResult(SAMPLE_ERROR_MESSAGE)
    );
    render(<App />);
    await uploadFile(user, createTestCsvFile());

    // Act
    await clickSubmitButton(user);

    // Assert
    await waitFor(() => {
      expect(screen.getByText(new RegExp(SAMPLE_ERROR_MESSAGE, 'i'))).toBeInTheDocument();
    });
  });

  it('should show loading state during upload', async () => {
    // Arrange
    const user = userEvent.setup();
    let resolveUpload: ((value: api.UploadResult) => void) | undefined;
    const uploadPromise: Promise<api.UploadResult> = new Promise((resolve) => {
      resolveUpload = resolve;
    });
    vi.mocked(api.uploadCsvFile).mockReturnValueOnce(uploadPromise);
    render(<App />);
    await uploadFile(user, createTestCsvFile());

    // Act
    await clickSubmitButton(user);

    // Assert
    assertLoadingStateIsActive();
    resolveUpload!(createMockEmptyResult());
    await waitFor(() => {
      expect(screen.queryByText(UI_PROCESSING_TEXT)).not.toBeInTheDocument();
    });
  });

  it('should handle PDF download', async () => {
    // Arrange
    const user = userEvent.setup();
    const mockBlob: Blob = new Blob([PDF_CONTENT], { type: APPLICATION_PDF_MIME_TYPE });
    vi.mocked(api.uploadCsvFile).mockResolvedValueOnce(createMockEmptyResult());
    vi.mocked(api.downloadPdf).mockResolvedValueOnce(mockBlob);
    global.URL.createObjectURL = vi.fn(() => BLOB_MOCK_URL);
    global.URL.revokeObjectURL = vi.fn();
    render(<App />);
    const file: File = createTestCsvFile();
    await uploadFile(user, file);
    await clickSubmitButton(user);
    await waitFor(() => {
      expect(screen.getByText(UI_DOWNLOAD_PDF_TEXT)).toBeInTheDocument();
    });
    const downloadButton: HTMLElement = screen.getByRole('button', { name: new RegExp(UI_DOWNLOAD_PDF_TEXT, 'i') });

    // Act
    await user.click(downloadButton);

    // Assert
    await waitFor(() => {
      expect(api.downloadPdf).toHaveBeenCalledWith(file);
    });
  });

  it('should format currency correctly', async () => {
    // Arrange
    const user = userEvent.setup();
    const netAmount: number = 1234567;
    const invoiceCount: number = 5;
    const mockResult: api.UploadResult = {
      success: true,
      data: {
        summariesByVatRate: [
          {
            vatRate: STANDARD_HUNGARIAN_VAT_RATE,
            totalNetAmount: netAmount,
            totalVatAmount: calculateVat(netAmount, STANDARD_HUNGARIAN_VAT_RATE),
            totalGrossAmount: calculateGross(netAmount, STANDARD_HUNGARIAN_VAT_RATE),
            invoiceCount: invoiceCount,
          },
        ],
        grandTotalNet: netAmount,
        grandTotalVat: calculateVat(netAmount, STANDARD_HUNGARIAN_VAT_RATE),
        grandTotalGross: calculateGross(netAmount, STANDARD_HUNGARIAN_VAT_RATE),
        totalInvoiceCount: invoiceCount,
      },
    };
    vi.mocked(api.uploadCsvFile).mockResolvedValueOnce(mockResult);
    render(<App />);
    await uploadFile(user, createTestCsvFile());

    // Act
    await clickSubmitButton(user);

    // Assert
    await waitFor(() => {
      const formattedAmounts: HTMLElement[] = screen.getAllByText(UI_CURRENCY_SUFFIX);
      expect(formattedAmounts.length).toBeGreaterThan(0);
    });
  });
});

async function uploadFile(user: ReturnType<typeof userEvent.setup>, file: File): Promise<void> {
  const fileInput: HTMLElement = screen.getByLabelText(UI_FILE_INPUT_LABEL);
  await user.upload(fileInput, file);
}

async function clickSubmitButton(user: ReturnType<typeof userEvent.setup>): Promise<void> {
  const submitButton: HTMLElement = screen.getByRole('button', { name: UI_SUBMIT_BUTTON_TEXT });
  await user.click(submitButton);
}

function getSubmitButton(): HTMLElement {
  return screen.getByRole('button', { name: UI_SUBMIT_BUTTON_TEXT });
}

function assertLoadingStateIsActive(): void {
  const processingButton: HTMLElement = screen.getByText(UI_PROCESSING_TEXT);
  expect(processingButton).toBeInTheDocument();
  expect(processingButton).toBeDisabled();
}

function createTestCsvFile(filename: string = SAMPLE_CSV_FILENAME): File {
  return new File([TEST_FILE_CONTENT], filename, { type: TEXT_CSV_MIME_TYPE });
}

function createMockVatResult(): api.UploadResult {
  const netAmount: number = 10000;

  return {
    success: true,
    data: {
      summariesByVatRate: [
        {
          vatRate: STANDARD_HUNGARIAN_VAT_RATE,
          totalNetAmount: netAmount,
          totalVatAmount: calculateVat(netAmount, STANDARD_HUNGARIAN_VAT_RATE),
          totalGrossAmount: calculateGross(netAmount, STANDARD_HUNGARIAN_VAT_RATE),
          invoiceCount: 1,
        },
      ],
      grandTotalNet: netAmount,
      grandTotalVat: calculateVat(netAmount, STANDARD_HUNGARIAN_VAT_RATE),
      grandTotalGross: calculateGross(netAmount, STANDARD_HUNGARIAN_VAT_RATE),
      totalInvoiceCount: 1,
    },
  };
}

function createMockErrorResult(errorMessage: string): api.UploadResult {
  return {
    success: false,
    error: errorMessage,
  };
}

function createMockEmptyResult(): api.UploadResult {
  return {
    success: true,
    data: {
      summariesByVatRate: [],
      grandTotalNet: 0,
      grandTotalVat: 0,
      grandTotalGross: 0,
      totalInvoiceCount: 0,
    },
  };
}

function calculateVat(netAmount: number, vatRate: number): number {
  return netAmount * vatRate / 100;
}

function calculateGross(netAmount: number, vatRate: number): number {
  return netAmount + calculateVat(netAmount, vatRate);
}
