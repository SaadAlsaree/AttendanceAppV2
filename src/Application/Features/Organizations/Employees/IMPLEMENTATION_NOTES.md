# Employees Feature Implementation Notes

## Overview

The Employees feature has been implemented following the same pattern as the Todos feature, with some adaptations to accommodate the more complex nature of the Employee entity and its relationships.

## Implemented Operations

### Create
- **Command**: `CreateEmployeeCommand` - Contains all employee properties from the README
- **Handler**: `CreateEmployeeCommandHandler` - Implements business rules for employee creation
- **Validator**: `CreateEmployeeCommandValidator` - Validates input parameters

### Get
- **Query**: `GetEmployeesQuery` - Supports filtering by organizational unit, manager status, and search term
- **Handler**: `GetEmployeesQueryHandler` - Returns a list of employees with related data
- **Response**: `EmployeeResponse` - Contains employee details including organizational unit and manager information

### GetById
- **Query**: `GetEmployeeByIdQuery` - Retrieves a single employee by ID
- **Handler**: `GetEmployeeByIdQueryHandler` - Returns detailed employee information

### Update
- **Command**: `UpdateEmployeeCommand` - Contains updatable employee properties
- **Handler**: `UpdateEmployeeCommandHandler` - Updates employee information and raises domain events
- **Validator**: `UpdateEmployeeCommandValidator` - Validates update parameters

### Delete
- **Command**: `DeleteEmployeeCommand` - Identifies employee to delete
- **Handler**: `DeleteEmployeeCommandHandler` - Implements soft delete with business rules
- **Validator**: `DeleteEmployeeCommandValidator` - Validates delete parameters

### AssignManager
- **Command**: `AssignManagerCommand` - Contains employee and manager IDs
- **Handler**: `AssignManagerCommandHandler` - Assigns manager with appropriate business rules
- **Validator**: `AssignManagerCommandValidator` - Validates assignment parameters

## Business Rules Implemented

1. Employee number must be unique
2. Email address must be unique
3. Employee must be assigned to an organizational unit
4. Manager assignment is optional
5. Full name is automatically generated from individual name components
6. Hire date cannot be in the future
7. Birth date must be valid (employee must be at least 18 years old)
8. Employee cannot be their own manager
9. Manager must be active and designated as a manager
10. Employee with subordinates cannot be deleted

## Domain Events

The following domain events are raised:

1. `EmployeeCreatedDomainEvent` - When a new employee is created
2. `EmployeeUpdatedDomainEvent` - When employee information is updated
3. `EmployeeManagerAssignedDomainEvent` - When a manager is assigned to an employee

## Deviations from Todos Pattern

1. **More Complex Entity**: The Employee entity has many more properties and relationships than the Todo entity
2. **Additional Business Rules**: Employee feature implements more complex business rules
3. **Additional Operation**: AssignManager operation is specific to the Employee feature
4. **Soft Delete**: Employee deletion is implemented as a soft delete with additional checks
5. **Relationship Validation**: Additional validation for organizational unit and manager relationships

## Notes

- All operations follow CQRS pattern with separate commands/queries and handlers
- Validation is implemented using FluentValidation
- Domain events are raised for important state changes
- Error handling follows the Result pattern from SharedKernel
