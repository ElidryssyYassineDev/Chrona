namespace TimeManagement.Domain;

// Pas de statut « Rejected » : un rejet est la transition Submitted -> Draft (06-domain-model.md, §5).
public enum TimesheetStatus { Draft, Submitted, Approved }