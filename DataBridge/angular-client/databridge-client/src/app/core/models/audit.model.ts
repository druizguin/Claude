export interface AuditRecord {
  id: string;
  timestamp: string;
  operationType: 'Create' | 'Read' | 'Update' | 'Delete';
  entityId: string;
  entityType: string;
  personName: string;
}
