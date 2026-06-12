type BooleanStatusProps = {
  value: boolean | null | undefined;
  trueLabel?: string;
  falseLabel?: string;
  unknownLabel?: string;
};

export function BooleanStatus({
  value,
  trueLabel = 'OK',
  falseLabel = 'Failed',
  unknownLabel = 'Unknown',
}: BooleanStatusProps) {
  if (value === null || value === undefined) {
    return (
      <span className="boolean-unknown">
        {unknownLabel}
      </span>
    );
  }

  return (
    <span className={value ? 'boolean-ok' : 'boolean-failed'}>
      {value ? trueLabel : falseLabel}
    </span>
  );
}