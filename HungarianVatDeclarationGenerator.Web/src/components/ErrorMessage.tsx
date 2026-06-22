/** @jsxImportSource react */

interface ErrorMessageProps {
  message: string;
}

export function ErrorMessage({ message }: ErrorMessageProps) {
  return (
    <div className="upload-form__error error-message" role="alert">
      <strong className="error-message__label">Error:</strong>
      <span className="error-message__text">{message}</span>
    </div>
  );
}
