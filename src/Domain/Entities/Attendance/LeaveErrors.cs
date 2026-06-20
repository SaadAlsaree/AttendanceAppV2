using SharedKernel;

namespace Domain.Entities.Attendance;

public static class LeaveErrors
{
    public static Error NotFound(Guid leaveId) => Error.NotFound(
        "Leave.NotFound",
        $"The leave request with Id = '{leaveId}' was not found");

    public static Error EmployeeNotFound(Guid employeeId) => Error.NotFound(
        "Leave.EmployeeNotFound",
        $"The employee with Id = '{employeeId}' was not found");

    public static Error InvalidDateRange(DateTime startDate, DateTime endDate) => Error.Problem(
        "Leave.InvalidDateRange",
        $"Invalid date range: start date '{startDate}' must be before end date '{endDate}'");

    public static Error StartDateInPast(DateTime startDate) => Error.Problem(
        "Leave.StartDateInPast",
        $"Start date '{startDate}' cannot be in the past");

    public static Error OverlappingLeave(Guid employeeId, DateTime startDate, DateTime endDate) => Error.Conflict(
        "Leave.OverlappingLeave",
        $"Leave request overlaps with existing leave for employee '{employeeId}' in date range '{startDate}' to '{endDate}'");

    public static Error InsufficientLeaveBalance(Guid employeeId, int requestedDays, int availableDays) => Error.Problem(
        "Leave.InsufficientLeaveBalance",
        $"Employee '{employeeId}' has insufficient leave balance. Requested: {requestedDays} days, Available: {availableDays} days");

    public static Error LeaveAlreadyApproved(Guid leaveId) => Error.Problem(
        "Leave.AlreadyApproved",
        $"The leave request with Id = '{leaveId}' is already approved");

    public static Error LeaveAlreadyRejected(Guid leaveId) => Error.Problem(
        "Leave.AlreadyRejected",
        $"The leave request with Id = '{leaveId}' is already rejected");

    public static Error LeaveAlreadyCancelled(Guid leaveId) => Error.Problem(
        "Leave.AlreadyCancelled",
        $"The leave request with Id = '{leaveId}' is already cancelled");

    public static Error CannotUpdateApprovedLeave(Guid leaveId) => Error.Problem(
        "Leave.CannotUpdateApprovedLeave",
        $"Cannot update leave request with Id = '{leaveId}' as it is already approved");

    public static Error CannotUpdateRejectedLeave(Guid leaveId) => Error.Problem(
        "Leave.CannotUpdateRejectedLeave",
        $"Cannot update leave request with Id = '{leaveId}' as it is already rejected");

    public static Error CannotDeleteApprovedLeave(Guid leaveId) => Error.Problem(
        "Leave.CannotDeleteApprovedLeave",
        $"Cannot delete leave request with Id = '{leaveId}' as it is already approved");

    public static Error InvalidLeaveType(int leaveType) => Error.Problem(
        "Leave.InvalidLeaveType",
        $"Invalid leave type: '{leaveType}'");

    public static Error InvalidLeaveStatus(int leaveStatus) => Error.Problem(
        "Leave.InvalidLeaveStatus",
        $"Invalid leave status: '{leaveStatus}'");

    public static Error UnauthorizedApproval(Guid leaveId) => Error.Forbidden(
        "Leave.UnauthorizedApproval",
        $"You are not authorized to approve leave request with Id = '{leaveId}'");

    public static Error UnauthorizedRejection(Guid leaveId) => Error.Forbidden(
        "Leave.UnauthorizedRejection",
        $"You are not authorized to reject leave request with Id = '{leaveId}'");

    public static Error RejectionReasonRequired() => Error.Validation(
        "Leave.RejectionReasonRequired",
        "Rejection reason is required when rejecting a leave request");

    public static Error LeaveHasStarted(Guid leaveId) => Error.Problem(
        "Leave.LeaveHasStarted",
        $"Cannot modify leave request with Id = '{leaveId}' as it has already started");

    public static Error LeaveHasEnded(Guid leaveId) => Error.Problem(
        "Leave.LeaveHasEnded",
        $"Cannot modify leave request with Id = '{leaveId}' as it has already ended");

    public static Error EditWindowExpired(Guid leaveId) => Error.Problem(
        "Leave.EditWindowExpired",
        "انتهت مدة تعديل الموقف. التعديل مسموح خلال 24 ساعة فقط من إنشائه.");

    public static Error UnauthorizedUpdate(Guid leaveId) => Error.Forbidden(
        "Leave.UnauthorizedUpdate",
        $"You are not authorized to update leave request with Id = '{leaveId}'");
}
