interface Props {
  value: string;
  onChange: (val: string) => void;
}

export default function ApiKeyField({ value, onChange }: Props) {
  return (
    <div className="form-row">
      <label htmlFor="apiKey">X-Api-Key</label>
      <input
        id="apiKey"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder="client-key-1"
      />
    </div>
  );
}
