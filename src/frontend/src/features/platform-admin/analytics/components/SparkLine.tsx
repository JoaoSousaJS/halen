interface SparkLineProps {
  data: number[];
  width?: number;
  height?: number;
  color?: string;
}

export default function SparkLine({
  data,
  width = 80,
  height = 24,
  color = 'var(--accent)',
}: SparkLineProps) {
  if (data.length === 0) {
    return <svg width={width} height={height} />;
  }

  const min = Math.min(...data);
  const max = Math.max(...data);
  const range = max - min || 1; // avoid division by zero when all values are equal

  const points = data
    .map((value, i) => {
      const x = data.length === 1 ? width / 2 : (i / (data.length - 1)) * width;
      const y = height - ((value - min) / range) * height;
      return `${x},${y}`;
    })
    .join(' ');

  return (
    <svg width={width} height={height}>
      <polyline points={points} fill="none" stroke={color} strokeWidth={1.5} />
    </svg>
  );
}
