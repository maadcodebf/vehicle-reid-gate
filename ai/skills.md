# Operational Skills (Runbooks)

## Skill 1: Onboard new barrier
1. Register barrier ID naming convention (e.g., B1, B2, B3).
2. Validate camera framing and crop quality.
3. Enroll sample passages.
4. Execute match tests against previous barrier.
5. Tune threshold if needed.

## Skill 2: Threshold calibration
1. Gather labeled dataset from real operations.
2. Split into positive (same truck) and negative (different truck).
3. Compute cosine distributions.
4. Set:
   - ThresholdHigh to keep false accepts low.
   - ThresholdLow to define ambiguous region.
5. Re-test weekly/monthly.

## Skill 3: Incident response for false rejects
1. Inspect top candidates and scores.
2. Verify timestamp UTC drift and barrier mapping.
3. Check image blur/occlusion.
4. Temporarily widen `TimeWindowMinutes` if operationally safe.
5. Retrain/fine-tune Re-ID model when recurrent.

## Skill 4: Incident response for false accepts
1. Raise ThresholdHigh for affected barrier pair.
2. Add extra constraints (lane/time rules).
3. Review hard-negative samples and refresh calibration set.
4. Consider secondary verification path.

## Skill 5: Model upgrade
1. Stage new ONNX model in non-production.
2. Validate InputName/OutputName/VectorSize.
3. Run A/B offline evaluation.
4. Deploy with rollback plan.
