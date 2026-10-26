Feature: Bilingual shopping through EasyShopper microservices
  @ui
  Scenario: Switch language in both directions and retain the choice
    Given I open the EasyShopper storefront
    When I select French
    Then the French shopping controls are displayed
    When I reload the storefront
    Then the French shopping controls are displayed
    When I select English
    Then the English shopping controls are displayed

  @api
  Scenario: A gateway command reaches MongoDB through RabbitMQ
    Given I submit a unique product through the gateway
    Then the exact product eventually exists in MongoDB
    And the gateway eventually returns that product

  @api
  Scenario: A gateway cart command reaches Redis through RabbitMQ
    Given I submit a unique product through the gateway
    And the gateway eventually returns that product
    When I submit a cart containing that product through the gateway
    Then the exact cart eventually exists in Redis
    And the gateway eventually returns that cart

  @ui
  Scenario: A French shopper finds a product and adds it to their cart
    Given I submit a unique product through the gateway
    And the gateway eventually returns that product
    And I open the EasyShopper storefront
    When I select French
    And I find my product and add it to the cart
    Then the exact cart eventually exists in Redis
    And the gateway eventually returns that cart

  @api
  Scenario: Invalid product identifiers are rejected
    Then the gateway rejects an invalid product identifier
