export type RiskLevel = 'Low' | 'Medium' | 'High' | 'Critical';

export interface RiskAssessment {
  id: string;
  servicePointId: string;
  score: number;
  level: RiskLevel;
  summary: string;
  evaluatedAtUtc: string;
}
