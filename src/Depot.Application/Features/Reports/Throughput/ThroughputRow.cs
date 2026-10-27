namespace Depot.Application.Features.Reports.Throughput;

// GateIns and GateOuts count the gate ins and gate outs of the operator's containers on that depot day (BR-RP-01).
public sealed record ThroughputRow(DateOnly Date, string OperatorCode, string OperatorName, int GateIns, int GateOuts);
