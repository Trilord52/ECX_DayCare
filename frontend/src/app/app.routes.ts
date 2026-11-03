import { Routes } from '@angular/router';
import { roleGuard, guestGuard } from './core/guards/auth.guard';
import { LoginComponent } from './pages/login/login.component';
import { AdminShellComponent } from './layout/admin-shell/admin-shell.component';
import { StaffShellComponent } from './layout/staff-shell/staff-shell.component';
import { ParentShellComponent } from './layout/parent-shell/parent-shell.component';

export const routes: Routes = [
  {
    path: 'login',
    title: 'Sign In | Little Steps Daycare',
    component: LoginComponent,
    canActivate: [guestGuard]
  },
  {
    path: 'admin',
    component: AdminShellComponent,
    canActivate: [roleGuard(['Admin'])],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', title: 'Dashboard | Little Steps Daycare', loadComponent: () => import('./pages/admin/dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent) },
      { path: 'attendance', title: 'Attendance | Little Steps Daycare', loadComponent: () => import('./pages/admin/attendance/admin-attendance.component').then(m => m.AdminAttendanceComponent) },
      { path: 'children', title: 'Children | Little Steps Daycare', loadComponent: () => import('./pages/admin/children/admin-children.component').then(m => m.AdminChildrenComponent) },
      { path: 'children/new', title: 'Add Child | Little Steps Daycare', loadComponent: () => import('./pages/admin/children/child-form.component').then(m => m.ChildFormComponent) },
      { path: 'children/:id/edit', title: 'Edit Child | Little Steps Daycare', loadComponent: () => import('./pages/admin/children/child-form.component').then(m => m.ChildFormComponent) },
      { path: 'children/:id', title: 'Child Profile | Little Steps Daycare', loadComponent: () => import('./pages/admin/children/child-detail.component').then(m => m.ChildDetailComponent) },
      { path: 'invoices', title: 'Invoices | Little Steps Daycare', loadComponent: () => import('./pages/admin/invoices/admin-invoices.component').then(m => m.AdminInvoicesComponent) },
      { path: 'invoices/:id', title: 'Invoice Details | Little Steps Daycare', loadComponent: () => import('./pages/admin/invoices/invoice-detail.component').then(m => m.InvoiceDetailComponent) },
      { path: 'staff', title: 'Staff | Little Steps Daycare', loadComponent: () => import('./pages/admin/staff/admin-staff.component').then(m => m.AdminStaffComponent) },
      { path: 'classrooms', title: 'Classrooms | Little Steps Daycare', loadComponent: () => import('./pages/admin/classrooms/admin-classrooms.component').then(m => m.AdminClassroomsComponent) },
      { path: 'guardians', title: 'Guardians | Little Steps Daycare', loadComponent: () => import('./pages/admin/guardians/admin-guardians.component').then(m => m.AdminGuardiansComponent) },
      { path: 'incidents', title: 'Incidents | Little Steps Daycare', loadComponent: () => import('./pages/admin/incidents/admin-incidents.component').then(m => m.AdminIncidentsComponent) },
      { path: 'announcements', title: 'Announcements | Little Steps Daycare', loadComponent: () => import('./pages/admin/announcements/admin-announcements.component').then(m => m.AdminAnnouncementsComponent) },
      { path: 'fee-plans', title: 'Fee Plans | Little Steps Daycare', loadComponent: () => import('./pages/admin/fee-plans/admin-fee-plans.component').then(m => m.AdminFeePlansComponent) },
      { path: 'reports', title: 'Reports | Little Steps Daycare', loadComponent: () => import('./pages/admin/reports/admin-reports.component').then(m => m.AdminReportsComponent) },
      { path: 'children/:id/log', title: 'Daily Log | Little Steps Daycare', loadComponent: () => import('./pages/staff/child-daily-log/child-daily-log.component').then(m => m.ChildDailyLogComponent) },
    ]
  },
  {
    path: 'staff',
    component: StaffShellComponent,
    canActivate: [roleGuard(['Staff'])],
    children: [
      { path: '', redirectTo: 'my-day', pathMatch: 'full' },
      { path: 'my-day', title: 'My Day | Little Steps Daycare', loadComponent: () => import('./pages/staff/my-day/staff-my-day.component').then(m => m.StaffMyDayComponent) },
      { path: 'children/:id/log', title: 'Daily Log | Little Steps Daycare', loadComponent: () => import('./pages/staff/child-daily-log/child-daily-log.component').then(m => m.ChildDailyLogComponent) },
      { path: 'announcements', title: 'Announcements | Little Steps Daycare', loadComponent: () => import('./pages/staff/announcements/staff-announcements.component').then(m => m.StaffAnnouncementsComponent) },
    ]
  },
  {
    path: 'parent',
    component: ParentShellComponent,
    canActivate: [roleGuard(['Parent'])],
    children: [
      { path: '', redirectTo: 'home', pathMatch: 'full' },
      { path: 'home', title: 'Home | Little Steps Daycare', loadComponent: () => import('./pages/parent/home/parent-home.component').then(m => m.ParentHomeComponent) },
      { path: 'children/:id', title: 'Child Today | Little Steps Daycare', loadComponent: () => import('./pages/parent/child-today/parent-child-today.component').then(m => m.ParentChildTodayComponent) },
      { path: 'incidents', title: 'Incidents | Little Steps Daycare', loadComponent: () => import('./pages/parent/incidents/parent-incidents.component').then(m => m.ParentIncidentsComponent) },
      { path: 'invoices', title: 'Invoices | Little Steps Daycare', loadComponent: () => import('./pages/parent/invoices/parent-invoices.component').then(m => m.ParentInvoicesComponent) },
      { path: 'announcements', title: 'Announcements | Little Steps Daycare', loadComponent: () => import('./pages/parent/announcements/parent-announcements.component').then(m => m.ParentAnnouncementsComponent) },
      { path: 'profile', title: 'Profile | Little Steps Daycare', loadComponent: () => import('./pages/parent/profile/parent-profile.component').then(m => m.ParentProfileComponent) },
    ]
  },
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  },
  {
    path: '**',
    redirectTo: 'login'
  }
];
