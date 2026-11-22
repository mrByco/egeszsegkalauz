import { Permission } from '../api/models';

export const PERMISSION_LABELS: Record<Permission, string> = {
  UsersView: 'Felhasználók megtekintése',
  UsersAssignRoles: 'Szerepkörök kiosztása felhasználóknak',
  RolesView: 'Szerepkörök megtekintése',
  RolesEdit: 'Szerepkörök létrehozása és szerkesztése',
};
