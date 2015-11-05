// Configuration for Dynamics CRM Graph
// ------------------------------
// You can customise this web resource and add your own custom entities

window.GraphOptions = {
    iterationCountPerLoad: 20,
    trace: false,
    demoModeInitialState: true,
    connectionFetchXml: "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false' nolock='true'>    <entity name='connection'>    <attribute name='record2id' />            <attribute name='record2roleid' />            <attribute name='connectionid' />            <attribute name='record1roleid' />            <attribute name='record1id' />            <order attribute='record2id' descending='false' />            <filter type='and'>              <condition attribute='record1id' operator='in'>                {0}              </condition>            </filter>          </entity>        </fetch>",
    acitvityFetchXml: "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true' no-lock='true' count='100'>    <entity name='activitypointer'>    <attribute name='activityid' />    <attribute name='activitytypecode' />    <attribute name='subject' alias='name'/>    <attribute name='modifiedon'/>    <attribute name='actualstart'/>    <attribute name='actualend'/>    <attribute name='scheduledstart'/>    <attribute name='scheduledend'/>    <attribute name='statecode'/>    <attribute name='regardingobjectid'/>          <attribute name='allparties' />       <order attribute='modifiedon' descending='true' />    <link-entity name='activityparty' from='activityid' to='activityid' alias='ab'>      <filter type='and'>        <condition attribute='partyid' operator='in'>         {0}        </condition>      </filter>    </link-entity>    </entity>    </fetch>",
    entities: {
        account: {
            displayName: "Accounts",
            logicalName: "account",
            nameAttribute: "name",
            idAttribute: "accountid",
            parentAttributeId: "parentaccountid",
            loadActivities: true,
            loadConnections: true,
            hierarchical: true,
            fetchXml: "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true' no-lock='true'>     <entity name='account'>    <attribute name='accountid'/>    <attribute name='name' />    <attribute name='telephone1'/>    <attribute name='emailaddress1'/>    <attribute name='ownerid'/>     <attribute name='parentaccountid'/>    <order attribute='name' descending='false' />      <filter type='and'>        <condition attribute='statecode' operator='eq' value='0' />       {0}      </filter>     </entity>     </fetch>",
            joins: [
                    {
                        leftEntity: "account",
                        rightEntity: "contact",
                        leftAttribute: "accountid",
                        rightAttribute: "parentcustomerid"
                    }
            ]
        },

        contact: {
            displayName: "Contacts",
            logicalName: "contact",
            nameAttribute: "fullname",
            idAttribute: "contactid",
            parentAttributeId: "parentcustomerid",
            loadActivities: true,
            loadConnections: true,
            hierarchical: false,
            fetchXml: "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true' no-lock='true'>     <entity name='contact'>    <attribute name='contactid'/>    <attribute name='fullname'  />    <attribute name='telephone1'/>    <attribute name='emailaddress1'/>    <attribute name='ownerid'/>    <attribute name='parentcustomerid'/>      <filter type='and'>           {0}    </filter>    <link-entity name='systemuser' from='internalemailaddress' to='emailaddress1' link-type='outer' >    <attribute name='systemuserid' alias='systemuserid' />     </link-entity>     </entity>     </fetch>",
            joins: [
                    {
                        leftEntity: "contact",
                        rightEntity: "account",
                        leftAttribute: "parentcustomerid",
                        rightAttribute: "accountid"
                    }
            ]
        },
        incident: {
            displayName: "Case",
            logicalName: "incident",
            nameAttribute: "title",
            idAttribute: "incidentid",
            parentAttributeId: "customerid",
            loadActivities: true,
            loadConnections: true,
            hierarchical: false,
            fetchXml: "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true' no-lock='true'>     <entity name='incident'>     <attribute name='incidentid'/>     <attribute name='ticketnumber' />  <attribute name='title' />   <attribute name='ownerid'/>    <attribute name='customerid'/>    <attribute name='modifiedon'/>     <filter type='and'>        <condition attribute='statecode' operator='eq' value='0' />    {0}    </filter>          </entity>     </fetch>",
            joins: []
        },
    },
    quickViewForms: {
        account: { address1_city: 'City', telephone1: 'Tel' },
        contact: { emailaddress1: 'Email', telephone1: 'Tel' },
        incident: { ticketnumber: 'CaseId', title: 'Title' },
        letter: { modifiedon: 'Modified', statecode: 'Status', actualedend: 'Due', regardingobjectid: 'Regarding' },
        email: { modifiedon: 'Modified', statecode: 'Status', actualend: 'Sent', regardingobjectid: 'Regarding' },
        phonecall: { modifiedon: 'Modified', statecode: 'Status', scheduledend: 'Due' },
        appointment: { statecode: 'Status', scheduledstart: 'Start', scheduledend: 'End', regardingobjectid: 'Regarding' },
        task: { modifiedon: 'Modified', statecode: 'Status', scheduledend: 'Due' }
    }
};



    
