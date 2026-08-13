# Testing Results

Each test run performed by the **reid-tester** agent (see `.github/agents/reid-tester.agent.md`)
produces one report file here, named:

```
YYYY-MM-DD_HHmmss-run.md
```

A follow-up agent (or a human) reads the most recent report to see what passed/failed and
resolve any open issues. Do not edit past reports — always create a new one per run so there
is a history of results over time.

## Report contents

Each report must include:

1. **Environment** - API base URL, Qdrant URL, collection name, points_count before/after.
2. **Enroll results** - one row per plate in `testing/enroll.http`: HTTP status, `storedEmbeddings`,
   `passageEventId`.
3. **Qdrant validation** - proof (via direct Qdrant REST calls) that each enrolled plate exists
   in the collection with a vector and the expected `license_plate` payload.
4. **Match results** - one row per file in `dataset/test`: expected outcome (from filename),
   actual `decision` / `matchedLicensePlate` / `bestScore`, and PASS/FAIL.
5. **Issues found** - a bullet list of any mismatches, errors, or suspicious scores (e.g.
   decisions landing in `UNCERTAIN` when a clear MATCH/NO_MATCH was expected), so the next
   agent has concrete leads to investigate.
