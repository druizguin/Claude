Feature: Audit Creation
    As a system administrator
    I want to create audit entries
    So that I can track changes to entities

    Background:
        Given the audit system is initialized

    Scenario: Create a simple audit entry
        Given I have a valid audit entry for entity "Persona" with action "Create"
        And the user id is "user-001"
        When I create the audit entry
        Then the audit entry should be saved successfully
        And the returned id should not be empty
        And the audit timestamp should be set automatically

    Scenario: Create an audit entry with property changes
        Given I have a valid audit entry for entity "Producto" with action "Update"
        And the user id is "user-002"
        And the audit has a property change for "Precio" from "100" to "150"
        And the audit has a property change for "Stock" from "10" to "25"
        When I create the audit entry
        Then the audit entry should be saved successfully
        And the audit should have 2 detail records

    Scenario: Retrieve an audit entry by id after creation
        Given I have a valid audit entry for entity "Persona" with action "Delete"
        And the user id is "user-003"
        When I create the audit entry
        And I retrieve the audit by its id
        Then the retrieved audit should not be null
        And the retrieved audit entity name should be "Persona"
        And the retrieved audit action should be "Delete"
