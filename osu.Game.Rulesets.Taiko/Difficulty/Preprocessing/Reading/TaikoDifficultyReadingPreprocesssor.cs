using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;

namespace osu.Game.Rulesets.Taiko.Difficulty.Preprocessing.Reading
{
    public class TaikoReadingDifficultyPreprocessor
    {
        public static void ProcessAndAssign(IBeatmap beatmap, List<TaikoDifficultyHitObject> hitObjects)
        {
            double beatmapGlobalSv = beatmap.Difficulty.SliderMultiplier / 1.4;
            using IEnumerator<TimingControlPoint> controlPointEnumerator = beatmap.ControlPointInfo.TimingPoints.GetEnumerator();
            controlPointEnumerator.MoveNext();
            TimingControlPoint currentControlPoint = controlPointEnumerator.Current;
            TimingControlPoint? nextControlPoint = controlPointEnumerator.MoveNext() ? controlPointEnumerator.Current : null;

            using IEnumerator<TaikoDifficultyHitObject> hitObjectEnumerator = hitObjects.GetEnumerator();

            while (hitObjectEnumerator.MoveNext())
            {
                TaikoDifficultyHitObject currentHitObject = hitObjectEnumerator.Current;

                if (nextControlPoint != null && currentHitObject.StartTime > nextControlPoint.Time)
                {
                    currentControlPoint = nextControlPoint;
                    nextControlPoint = controlPointEnumerator.MoveNext() ? controlPointEnumerator.Current : null;
                }

                double effectiveBPM = currentControlPoint.BPM * beatmapGlobalSv * currentHitObject.BaseObject.DifficultyControlPoint.SliderVelocity;
                currentHitObject.Reading.EffectiveBPM = effectiveBPM;
            }
        }
    }
}
