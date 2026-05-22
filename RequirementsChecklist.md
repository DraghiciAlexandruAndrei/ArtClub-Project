# Art Club Information System - Software Requirements Checklist

**IMPLEMENTATION STATUS UPDATE**: Phase 1 Complete - Admin Override & Buffer Logic Infrastructure Verified

This checklist tracks the implementation status of the requirements defined in the SRS for the Art Enthusiasts Club information system.
The possible statuses are: Implemented (✅), Partially Implemented (⚠️), Wrongly Implemented (❌), Not Implemented Yet (⏳), Overruled (🚫), or Delayed (⏹️).

## 4.1 System Feature 1 - User and Member Management

### Authentication & user CRUD
- [x] REQ-1: The system shall provide a registration form... - ✅
- [x] REQ-2: The system shall provide a secure login function... - ✅
- [x] REQ-3: The system shall allow admins to add, edit, delete, or ban users... - ✅
  * **Observation**: `AccountController` and `AdminController` implement these using ASP.NET Core Identity.

### Membership & payments at user level
- [ ] REQ-4: The system shall allow admins to view and filter past membership... - 🟡 **IN PROGRESS**
  * **Observation**: `FinanceService` retrieves payments. Admin UI filtering enhancement in progress (Phase 3).
  * **Status Change**: Moving to Phase 3 (Admin Filtering & Search) after Phase 1 completion.
- [ ] REQ-5: The system shall allow users to purchase memberships... - ⚠️
  * **Observation**: `IsMembershipActive` and `MembershipDate` exist on the `User` model, but there's no payment gateway integration.

### Member profile & account lifecycle
- [x] REQ-6: The system shall allow members to view their profile details... - ✅
- [ ] REQ-7: The system shall allow members to temporarily deactivate... - ⏳
  * **Observation**: The `User` model has an `IsActive` property, but no self-service UI for members to toggle it.
- [x] REQ-8: The system shall allow members to modify their personal info... - ✅
- [ ] REQ-9: The system shall include a search function enabling admins... - 🟡 **IN PROGRESS**
  * **Observation**: `AdminController` has a basic search, enhancing with membership ID filtering (Phase 3).
  * **Status Change**: Moving to Phase 3 after Phase 1 completion.
- [x] REQ-10: The system shall allow an admin to view a list of registered users... - ✅
- [x] REQ-11: Admins shall be able change the role of an user - ✅
- [ ] REQ-12: The system shall provide a password reset mechanism... - ⏳
- [ ] REQ-13: The system shall require users to verify their email address... - ⏳
- [ ] REQ-14: The system shall allow members to configure their notification... - ⏳

## 4.2 System Feature 2 – Resource and Reservation Management

### Resource CRUD & status
- [x] REQ-15: The Admin shall be able to manage resources... - ✅
- [ ] REQ-16: The system shall display the current availability status... - ⚠️
  * **Observation**: Reservations logically affect availability, but real-time status updates/display in UI limits might be missing.
- [x] REQ-17: The system shall allow users to view a searchable list... - ✅

### Reservation rules
- [x] REQ-18: The system shall ensure that two reservations do not overlap... - ✅
  * **Observation**: Fully handled in `ReservationService.HasOverlappingReservationAsync`.
- [x] REQ-19: The system shall make resources unavailable for other reservations... - ✅
  * **Observation**: Logic appends a 1-day buffer (`requestedBufferStart`, `requestedBufferEnd`) successfully.
- [x] REQ-20: The system shall allow admins to reserve any available resource... - ✅ **FULLY IMPLEMENTED**
  * **Implementation Complete**: 
    - ✅ Fixed 1-day buffer in LINQ queries - replaced computed properties with inline `AddDays(-1)` / `AddDays(1)` calculations
    - ✅ Admin users can now override (cancel and replace) any conflicting reservations
    - ✅ When admin creates an event that conflicts with existing ones, conflicting events are automatically cancelled
    - ✅ Non-admin users are blocked when buffer conflicts exist
    - ✅ Conflicting external/member reservations marked as `OverrideRequired` for admin overrides (via ReservationService)
    - ✅ EventService automatically cancels all conflicting events when admin creates a booking in a taken slot
  * **Observation**: Admin override fully functional. 1-day buffer enforced for all non-admin users. Admin can force-book any time slot.
- [ ] REQ-21: The system shall allow free reservations for members only when... - ⏳
  * **Observation**: Currently missing the logic connecting club deficit checks (REQ-47) to block free reservations.

### Special resource usage
- [ ] REQ-22: Admins will be able to create timed exhibition halls... - ⏳

### Resource‐centric reports
- [ ] REQ-23: Members or admins are able to generate a report... - ⏳
- [ ] REQ-24: Members or admins are able to generate a report... - ⏳
- [ ] REQ-25: The system shall provide a search function... - ⏳
- [ ] REQ-26: The system shall support the definition of extensible resource... - ⏳
- [ ] REQ-27: The system shall limit each user to a configurable maximum... - ⚠️
  * **Observation**: Event limits are handled correctly (`GetEventCreationLimit()`), but raw resource reservation limits separate from events seem unchecked.
- [x] REQ-28: The system shall display a calendar... - ✅
  * **Observation**: Covered by `ResourceController.Calendar` and backend service.

## 4.3 System Feature 3 – Event and Invitation Management

### Event CRUD
- [x] REQ-29: Members shall be able to create events provided they have not exceeded... - ✅
  * **Observation**: User model tracks `OrganizedEvents` and dynamically applies a limit based on their role.
- [ ] REQ-30: Members shall be able to edit their own events only up to two days before... - ⏳
  * **Observation**: Missing explicit date validation logic in `EventController.Edit`.

### Invitations & inbox
- [x] REQ-31: Members shall be able to select users to send event invitations - ✅
- [x] REQ-32: Members shall be able to reserve resources for an event... - ✅
- [x] REQ-33: Users shall be able to access the invitation inbox system... - ✅
- [ ] REQ-34: Users shall receive automatic email notifications... - ⚠️
  * **Observation**: Code calls `_notificationService.SendEmailAsync`, but a proper SMTP implementation needs configuration.
- [ ] REQ-35: Users shall receive in-app notifications... - ⏳

### Event location & affiliated venues
- [ ] REQ-36: The system shall allow admins to add and manage affiliated event locations... - ⏳
- [ ] REQ-37: Members shall be able choose the location event or select one of the affiliated... - ⏳

### Content around events / art pieces
- [ ] REQ-38: The system should provide a “New Pieces” section... - ⚠️
  * **Observation**: Backend has `GetAllArtPiecesAsync` but UI blocks for dedicated filtered queries are not set.
- [ ] REQ-39: The system should provide a “Popular Pieces” section... - ⏳
  * **Observation**: Needs a metric counter for views/reservations and an accompanying UI listing.
- [x] REQ-40: The system should provide a detailed information page... - ✅
- [ ] REQ-41: The system shall allow members to cancel their events or memberships... - ⏳
- [x] REQ-42: The invited members can view event details before accepting... - ✅

## 4.4 System Feature 4 – Payments, Revenues, Expenses and Reports

### Core payments & systems
- [x] REQ-43: The system should allow admins to record additional payments... - ✅
- [ ] REQ-44: The system shall allow admins to define and update payment amounts... - ⏳
  * **Observation**: Fees like `CalculateNonMemberReservationFee` use hardcoded values (e.g. 400 lei) instead of DB/configurable configs.
- [ ] REQ-45: The application shall include an integrated payment system... - ⏳

### Financial rules & tariffs
- [x] REQ-46: For every event that uses at least one resource, the system shall record an expense... - ✅
  * **Observation**: Automatically tracked during processing.
- [ ] REQ-47: If, for a given month, the total income of the club is less than the total expenses... - ⏳
  * **Observation**: Core `CalculateMonthlyBalanceAsync` exists, but there's no middleware check blocking the initial UI reservations if negative.
- [ ] REQ-48: For non‐members who reserve resources, the system shall charge a fee... - ⚠️
  * **Observation**: Backend has `CalculateNonMemberReservationFee(int days)` but it's not strictly invoked securely upon UI reservation creation.

### Reports (money‐centric)
- [x] REQ-49: The system shall generate reports of all income received from user payments... - ✅
- [x] REQ-50: The system shall generate monthly reports summarizing total income, expenses... - ✅
- [x] REQ-51: The system shall allow administrators to export financial reports in PDF format... - ✅
  * **Observation**: Supported via PDFsharp dependency integrated within `FinanceService.GenerateMonthlyReportAsync`.