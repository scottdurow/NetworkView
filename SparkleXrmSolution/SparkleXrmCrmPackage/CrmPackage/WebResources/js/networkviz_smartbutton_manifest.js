[
  {
      "ControlType": 0,
      "__type": "ButtonControl",
      "Id": "smartbutton.networkviz",
      "Image16by16": "$webresource:dev1_/images/networkviz16.png",
      "LabelTextText": "Vizualize",
      "Labels": [],
      "smartButtonId": "runworkflow",
      "definitions": [
        {
            "validOnForm": true,
            "validOnHomePageGrid": false,
            "validOnSubGrid": false,
            "template": {
                "Control": {
                    "__type": "ButtonControl",
                    "Id": "dev1.NetworkViz.Button",
                    "Labels": [
                      {
                          "Id": "Alt",
                          "LCID": 1033,
                          "Text": null
                      },
                      {
                          "Id": "LabelText",
                          "LCID": -1,
                          "Text": "$LocLabels:dev1.NetworkViz.Button.LabelText"
                      },
                      {
                          "Id": "ToolTipTitle",
                          "LCID": -1,
                          "Text": "$LocLabels:dev1.NetworkViz.Button.ToolTipTitle"
                      },
                      {
                          "Id": "ToolTipDescription",
                          "LCID": -1,
                          "Text": "$LocLabels:dev1.NetworkViz.Button.ToolTipDescription"
                      },
                      {
                          "Id": "LabelText",
                          "LCID": 1033,
                          "Text": "Visualize"
                      },
                      {
                          "Id": "ToolTipTitle",
                          "LCID": 1033,
                          "Text": "Visualize"
                      },
                      {
                          "Id": "ToolTipDescription",
                          "LCID": 1033,
                          "Text": "Visualize the relationships to other records"
                      }
                    ],
                    "CommandCore": "dev1.NetworkViz.Command",
                    "ControlType": 0,
                    "Image16by16": "$webresource:dev1_/images/networkviz16.png",
                    "Image32by32": null,
                    "LabelTextText": "Visualize",
                    "TemplateAlias": "o1",
                    "ToolTipDescriptionText": "Visualize the relationships to other records",
                    "ToolTipTitleText": "Visualize",
                    "AltText": null,
                    "ModernImage": null,
                    "Description": null
                },
                "CommandDefinition": {
                    "Id": "dev1.NetworkViz.Command",
                    "Labels": [],
                    "Actions": [
                      {
                          "__type": "JavascriptFunctionCommandAction",
                          "Parameters": [],
                          "FunctionName": "isNaN",
                          "Library": "$webresource:sparkle_/js/mscorlib_crm.js"
                      },
                      {
                          "__type": "JavascriptFunctionCommandAction",
                          "Parameters": [],
                          "FunctionName": "isNaN",
                          "Library": "$webresource:sparkle_/js/SparkleXrm.js"
                      },
                      {
                          "__type": "JavascriptFunctionCommandAction",
                          "Parameters": [],
                          "FunctionName": "NetworkView.ClientHooks.Ribbon.RibbonCommands.openNetworkViewFromForm",
                          "Library": "$webresource:dev1_/js/NetworkViewClientHooks.js"
                      }
                    ],
                    "DisplayRuleIds": [],
                    "EnableRuleIds": []
                },
                "DisplayRules": [],
                "EnableRules": []
            },
            "propertyExpressions": [
              {
                  "name": "$button.LabelTextText",
                  "value": "$data.Title"
              }
            ]
        },
        {
            "validOnForm": false,
            "validOnHomePageGrid": true,
            "validOnSubGrid": true,
            "template": {
                "Control": {
                    "__type": "ButtonControl",
                    "Id": "dev1.NetworkViz.GridButton.Button",
                    "Labels": [
                      {
                          "Id": "Alt",
                          "LCID": 1033,
                          "Text": null
                      },
                      {
                          "Id": "LabelText",
                          "LCID": -1,
                          "Text": "$LocLabels:dev1.NetworkViz.GridButton.Button.LabelText"
                      },
                      {
                          "Id": "ToolTipTitle",
                          "LCID": -1,
                          "Text": "$LocLabels:dev1.NetworkViz.GridButton.Button.ToolTipTitle"
                      },
                      {
                          "Id": "ToolTipDescription",
                          "LCID": -1,
                          "Text": "$LocLabels:dev1.NetworkViz.GridButton.Button.ToolTipDescription"
                      },
                      {
                          "Id": "LabelText",
                          "LCID": 1033,
                          "Text": "Visualize"
                      },
                      {
                          "Id": "ToolTipTitle",
                          "LCID": 1033,
                          "Text": "Visualize"
                      },
                      {
                          "Id": "ToolTipDescription",
                          "LCID": 1033,
                          "Text": "Visualize the relationships to other records"
                      }
                    ],
                    "CommandCore": "dev1.NetworkViz.Grid.Command",
                    "ControlType": 0,
                    "Image16by16": "$webresource:dev1_/images/networkviz16.png",
                    "Image32by32": null,
                    "LabelTextText": "Visualize",
                    "TemplateAlias": "o1",
                    "ToolTipDescriptionText": "Visualize the relationships to other records",
                    "ToolTipTitleText": "Visualize",
                    "AltText": null,
                    "ModernImage": null,
                    "Description": null
                },
                "CommandDefinition": {
                    "Id": "dev1.NetworkViz.Grid.Command",
                    "Labels": [],
                    "Actions": [
                      {
                          "__type": "JavascriptFunctionCommandAction",
                          "Parameters": [],
                          "FunctionName": "isNaN",
                          "Library": "$webresource:sparkle_/js/mscorlib_crm.js"
                      },
                      {
                          "__type": "JavascriptFunctionCommandAction",
                          "Parameters": [],
                          "FunctionName": "isNaN",
                          "Library": "$webresource:sparkle_/js/SparkleXrm.js"
                      },
                      {
                          "__type": "JavascriptFunctionCommandAction",
                          "Parameters": [
                            {
                                "__type": "CrmParameter",
                                "Name": null,
                                "Value": 8
                            },
                            {
                                "__type": "CrmParameter",
                                "Name": null,
                                "Value": 7
                            }
                          ],
                          "FunctionName": "NetworkView.ClientHooks.Ribbon.RibbonCommands.openNetworkView",
                          "Library": "$webresource:dev1_/js/NetworkViewClientHooks.js"
                      }
                    ],
                    "DisplayRuleIds": [],
                    "EnableRuleIds": [
                      "dev1.account.SingleItemSelected.EnableRule"
                    ]
                },
                "DisplayRules": [],
                "EnableRules": [
                  {
                      "Id": "dev1.account.SingleItemSelected.EnableRule",
                      "Labels": [],
                      "IsCore": false,
                      "Steps": [
                        {
                            "__type": "SelectionCountRule",
                            "Default": true,
                            "InvertResult": null,
                            "AppliesTo": 1,
                            "Maximum_": "1",
                            "Minimum_": "1"
                        }
                      ],
                      "__type": "EnableRule"
                  }
                ]
            },
            "propertyExpressions": [
              {
                  "name": "$button.LabelTextText",
                  "value": "$data.Title"
              }
            ]
        }
      ],
      "editableProperties": [
        {
            "Label": "Title",
            "Value": null,
            "ColSpan": 2,
            "FieldName": "Title",
            "PropertyType": "text",
            "onlyOnCreate": true,
            "Options": null,
            "QueryCommand": null,
            "IdAttribute": null,
            "NameAttribute": null,
            "Disable": false,
            "Precision": 0
        }
      ]
  }
]