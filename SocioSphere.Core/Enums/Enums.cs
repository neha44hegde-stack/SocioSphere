namespace SocioSphere.Core.Enums;

public enum FlatStatus { VACANT, OCCUPIED }
public enum BillStatus { PENDING, PARTIALLY_PAID, PAID, OVERDUE }
public enum PaymentMethod { UPI, BANK_TRANSFER, CASH, CHEQUE, OTHER }
public enum PaymentStatus { SUCCESS, PENDING, FAILED }
public enum ComplaintCategory { PLUMBING, ELECTRICAL, CLEANING, SECURITY, LIFT, WATER, PARKING, OTHER }
public enum ComplaintPriority { LOW, MEDIUM, HIGH, URGENT }
public enum ComplaintStatus { OPEN, ASSIGNED, IN_PROGRESS, RESOLVED, CLOSED }
public enum NoticeCategory { GENERAL, MAINTENANCE, SECURITY, EVENT, EMERGENCY }
public enum NoticePriority { NORMAL, HIGH, URGENT }
public enum BookingStatus { CONFIRMED, CANCELLED, COMPLETED }
public enum VisitorType { GUEST, DELIVERY, CAB, SERVICE_PROVIDER, OTHER }
public enum VisitorStatus { EXPECTED, CHECKED_IN, CHECKED_OUT, CANCELLED }
public enum ParkingType { TWO_WHEELER, FOUR_WHEELER }
public enum ParkingStatus { ALLOCATED, AVAILABLE }
public enum ExpenseCategory { ELECTRICITY, SECURITY, CLEANING, LIFT_MAINTENANCE, REPAIRS, WATER, GARDENING, OTHER }
public enum StaffRole { SECURITY_GUARD, CLEANER, MAINTENANCE_STAFF, MANAGER, OTHER }
public enum VendorStatus { ACTIVE, INACTIVE }
public enum RelationshipType { OWNER, TENANT, FAMILY_MEMBER }
