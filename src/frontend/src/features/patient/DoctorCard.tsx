import type { DoctorSearchDto } from '../../shared/api/doctors';
import { Button, Chip } from '../../shared/components';

interface DoctorCardProps {
  doctor: DoctorSearchDto;
  onSelect: (doctor: DoctorSearchDto) => void;
}

export default function DoctorCard({ doctor, onSelect }: DoctorCardProps) {
  return (
    <article className="doctor-card" aria-label={`${doctor.name}, ${doctor.specialty}`}>
      <div className="doctor-card-header">
        <span className="doctor-card-name">{doctor.name}</span>
        <Chip status={doctor.specialty} />
      </div>

      <div className="doctor-card-info">
        <span className="doctor-card-fee" aria-label={`Consultation fee: $${doctor.consultationFee}`}>
          ${doctor.consultationFee}
        </span>
        <span className="doctor-card-meta">{doctor.yearsOfExperience} years experience</span>
      </div>

      <div className="doctor-card-languages" aria-label={`Languages: ${doctor.languages.join(', ')}`}>
        {doctor.languages.join(', ')}
      </div>

      <div className="doctor-card-availability">
        {doctor.nextAvailableSlot ? (
          <span>Next: {doctor.nextAvailableSlot.dayOfWeek}</span>
        ) : (
          <span className="text-dim">No upcoming availability</span>
        )}
      </div>

      <Button variant="primary" size="sm" ariaLabel={`Select ${doctor.name}`} onClick={() => onSelect(doctor)}>
        Select
      </Button>
    </article>
  );
}
