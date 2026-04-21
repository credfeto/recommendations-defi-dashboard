export interface Predictions {
  predictedClass: string | null;
  predictedProbability: number | null;
  binnedConfidence: number | null;
}
