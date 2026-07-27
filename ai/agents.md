# AI Agents for Vehicle Re-ID Gate

This project can be operated with role-based AI assistants (or human equivalents):

## 1) ReID-Integrator Agent
**Goal:** integrate camera/barrier events into API calls.

Responsibilities:
- Build payloads for `/api/reid/enroll` and `/api/reid/match`
- Ensure timestamps are UTC
- Validate image quality and base64 integrity

## 2) ReID-Calibrator Agent
**Goal:** optimize `ThresholdHigh` / `ThresholdLow`.

Responsibilities:
- Collect labeled positive/negative pairs
- Evaluate score distributions
- Recommend per-barrier thresholds

## 3) ReID-SRE Agent
**Goal:** keep service healthy in production.

Responsibilities:
- Monitor API error rate and latency
- Monitor Qdrant availability and volume usage
- Validate backup and restore procedures

## 4) ReID-Security Agent
**Goal:** secure exposure of API and data.

Responsibilities:
- Enforce authN/authZ controls
- Validate secrets handling and TLS
- Review logs for sensitive data exposure
