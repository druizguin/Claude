Feature: Audit Retrieval
    As a system administrator
    I want to query audit entries
    So that I can investigate changes to entities

    Background:
        Given the audit system is initialized

    Scenario: Get audits by user id
        Given I have created 3 audit entries for user "test-user-bdd"
        When I query audits for user "test-user-bdd"
        Then the result should contain 3 entries
        And all entries should belong to user "test-user-bdd"

    Scenario: Get paginated results
        Given I have created 5 audit entries for user "paged-user"
        When I query audits with page size 2 and page 1
        Then the result should contain at most 2 entries

    Scenario: Get total count
        Given I have created 4 audit entries for user "count-user"
        When I get the total count of audits for user "count-user"
        Then the count should be at least 4

    Scenario: Get audit details with property changes
        Given I have created an audit with a property change for "Nombre" from "Juan" to "Pedro"
        When I retrieve the audit by its id
        Then the audit details should contain a change for property "Nombre"
        And the old value for "Nombre" should be "Juan"
        And the new value for "Nombre" should be "Pedro"
