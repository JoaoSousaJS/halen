import type { RegionDto } from '../../../../shared/api/analytics';

interface IberiaMapProps {
  regions: RegionDto[];
}

const CITY_POSITIONS: Record<string, [number, number]> = {
  Lisbon: [130, 340],
  Lisboa: [130, 340],
  Porto: [100, 200],
  Faro: [120, 420],
  Coimbra: [110, 270],
  Braga: [95, 170],
  Aveiro: [90, 240],
  Évora: [150, 370],
  Setúbal: [140, 360],
  Funchal: [60, 480],
  Madrid: [280, 290],
  Barcelona: [430, 200],
  Seville: [210, 400],
  Valencia: [370, 310],
  Bilbao: [240, 130],
};

const MIN_RADIUS = 6;
const MAX_RADIUS = 24;

export default function IberiaMap({ regions }: IberiaMapProps) {
  const maxConsults = Math.max(1, ...regions.map((r) => r.consults));

  const known = regions.filter((r) => CITY_POSITIONS[r.name]);
  const unknown = regions.filter((r) => !CITY_POSITIONS[r.name]);

  return (
    <div>
      <svg className="analytics-map" viewBox="0 0 500 520" role="img" aria-label="Iberia map">
        <path
          d="M80,80 L200,60 L350,80 L450,140 L470,220 L440,300 L400,350 L350,380 L280,420 L200,440 L140,440 L100,400 L80,350 L60,280 L50,200 L60,140 Z"
          fill="none"
          stroke="var(--border)"
          strokeWidth="1.5"
        />
        {known.map((region) => {
          const [cx, cy] = CITY_POSITIONS[region.name];
          const ratio = region.consults / maxConsults;
          const r = MIN_RADIUS + ratio * (MAX_RADIUS - MIN_RADIUS);
          return (
            <circle
              key={region.name}
              className="analytics-map-bubble"
              cx={cx}
              cy={cy}
              r={r}
            >
              <title>{`${region.name}: ${region.consults}`}</title>
            </circle>
          );
        })}
      </svg>

      {unknown.length > 0 && (
        <div className="analytics-region-list">
          {unknown.map((region) => (
            <div key={region.name} className="analytics-region-item">
              <span>{region.name}</span>
              <span>{region.consults}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
