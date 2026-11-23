import { HealthDataSource } from '../api/models';
import { HEALTH_DATA_SOURCE } from '../api/models/health-data-source-array';

export const SOURCE_LABELS: Record<HealthDataSource, string> = {
  Garmin: 'Garmin Connect',
  Hevy: 'Hevy',
};

export function sourceFromPath(segment: string | undefined): HealthDataSource | undefined {
  return HEALTH_DATA_SOURCE.find((source) => source.toLowerCase() === segment?.toLowerCase());
}
