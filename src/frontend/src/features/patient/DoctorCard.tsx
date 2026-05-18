import type { DoctorSearchDto } from '../../shared/api/doctors';
import { Button, Chip } from '../../shared/components';
import { renderStars } from '../../shared/utils/renderStars';

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

      {doctor.averageRating != null && (
        <div className="doctor-card-rating" aria-label={`Rating: ${doctor.averageRating.toFixed(1)} out of 5, ${doctor.reviewCount} reviews`}>
          <span className="doctor-card-stars">{renderStars(doctor.averageRating)}</span>
          <span className="doctor-card-rating-text">{doctor.averageRating.toFixed(1)}</span>
          <span className="doctor-card-review-count">({doctor.reviewCount} reviews)</span>
          {doctor.averageRating >= 4.7 && doctor.reviewCount >= 50 && (
            <Chip status="Top rated" />
          )}
        </div>
      )}

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
