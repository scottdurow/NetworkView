// NetworkViewModel.c
//

using ClientUI.D3Api;
using KnockoutApi;
using SparkleXrm;
using SparkleXrm.GridEditor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Html;
using System.Runtime.CompilerServices;
using Xrm;
using Xrm.Sdk;

namespace ClientUI.ViewModels
{

    public delegate void NewNodesHandler(object sender, EventArgs e);
    public delegate void ZoomHandler(object sender, int direction);

    public class NetworkViewModel : ViewModelBase
    {
        #region Fields
        public GraphOptions Config;
        [PreserveCase]
        public Observable<bool> DemoMode = Knockout.Observable<bool>(true);
        private int DemoCounter = 0;
        [PreserveCase]
        public Observable<string> SelectedUserId;
        [PreserveCase]
        public Observable<int> Iterations = Knockout.Observable<int>(0);
        [PreserveCase]
        public Observable<bool> LoadIsPaused = Knockout.Observable<bool>(false);
        public Guid RootEntityId;
        public string RootEntityLogicalName;
        public QueuedLoad _currentLoad;
        public List<EntityNode> Nodes = new List<EntityNode>();
        public List<EntityLink> Links = new List<EntityLink>();

        public Queue<QueuedLoad> Queue = new Queue<QueuedLoad>();
        public Dictionary<string, EntityNode> idIndex = new Dictionary<string, EntityNode>();
        public Dictionary<string, string> idIndexQueried = new Dictionary<string, string>();
        public Dictionary<string, EntityLink> linkIndex = new Dictionary<string, EntityLink>();
        public Dictionary<string, EntityNode> idIndexActivityLoad = new Dictionary<string, EntityNode>();
        public Dictionary<string, EntityNode> idIndexConnectionLoad = new Dictionary<string, EntityNode>();
        public Dictionary<string, string> EmailDomains = new Dictionary<string, string>();

        public bool SuspendLayout = false;
        [PreserveCase]
        public ObservableArray<UserOrTeam> UsersAndTeams;
        [PreserveCase]
        public ObservableArray<string> ConnectionRoles = Knockout.ObservableArray<string>();
        private Dictionary<string, UserOrTeam> UserIdIndex = new Dictionary<string, UserOrTeam>();
        private Dictionary<string, Dictionary<string, EntityLink>> ConnectionRoleIndex = new Dictionary<string, Dictionary<string, EntityLink>>();
        public ObservableArray<EntityReference> HighlightedEntities;
        public ObservableArray<EntityReference> HighlightedEntitiesToRemove = Knockout.ObservableArray<EntityReference>();
        [PreserveCase]
        public ObservableArray<string> HighlightedLinks = Knockout.ObservableArray<string>();
        public ObservableArray<string> HighlightedLinksToRemove = Knockout.ObservableArray<string>();
        private Dictionary<string, UserOrTeam> userOrTeamIds = new Dictionary<string, UserOrTeam>();
        private Dictionary<string, Dictionary<string, PendingLink>> pendingLinks = new Dictionary<string, Dictionary<string, PendingLink>>();
        private Date _lastInterrupt = null;

        [PreserveCase]
        public Observable<EntityNode> SelectedNode = Knockout.Observable<EntityNode>(new EntityNode(null, 0));
        [PreserveCase]
        public ObservableArray<string> SelectedConnectionRoles = Knockout.ObservableArray<string>();
        public Observable<bool> TooManyNodes = Knockout.Observable<bool>(false);
        public Dictionary<string, Entity> userContacts = new Dictionary<string, Entity>();
        public bool CancelRequested = false;
        private int MaxIterations = 10;
        private int MaxQueueItems = 1000;
        private int QueueIterations = 0;
        private int _startX = 0;
        private int _startY = 0;
        private int OverflowMax = 5;
        #endregion

        #region Events
        public event NewNodesHandler OnNewNodes;
        public event EventHandler OnSelectedNodesCleared;
        public event EventHandler OnSelectedNodesAdded;
        public event EventHandler OnSelectedLinksAdded;
        public event EventHandler OnInfoBoxClose;
        public event ZoomHandler OnZoom;
        #endregion

        #region Constructors
        public NetworkViewModel(Guid id, string logicalName, GraphOptions config)
        {

            UsersAndTeams = Knockout.ObservableArray<UserOrTeam>();
            HighlightedEntities = Knockout.ObservableArray<EntityReference>();
            SelectedUserId = Knockout.Observable<string>();
            RootEntityId = id;
            RootEntityId.Value = NormalisedGuid(RootEntityId.Value);
            RootEntityLogicalName = logicalName;
            Config = config;
            GetDefaultConfig();

            if (Type.GetScriptType(Config) == "undefined")
            {
                // Config is not supplied
                CancelRequested = true;
                Window.SetTimeout(delegate()
                {
                    ReportError(new Exception(ResourceStrings.NoConfigurationError));
                }, 100);
                return;
            }

            // Get default demo mode
            if ((Type.GetScriptType(Config.DemoModeInitialState) != "undefined"))
            {
                DemoMode.SetValue(config.DemoModeInitialState.Value);
            }

            // Get default max iterations
            if ((Type.GetScriptType(Config.IterationCountPerLoad) == "undefined"))
            {
                Config.IterationCountPerLoad = MaxIterations;
            }

            // Queue the root load
            if (Config.Entities != null && Config.Entities.Count > 0)
            {
                Queue.Enqueue(new QueuedLoad(new List<string>(new string[] { id.Value }), Config.Entities[logicalName], null));
            }

            DemoTick();
        }

        private void GetDefaultConfig()
        {

            //            // Add the default config
            //            Config = new NetworkViewOptions();
            //            Config.Trace = false;
            //            Config.Entities = new Dictionary<string, EntitySetting>();
            //            Config.ConnectionFetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false' nolock='true'>
            //                          <entity name='connection'>
            //                            <attribute name='record2id' />
            //                            <attribute name='record2roleid' />
            //                            <attribute name='connectionid' />
            //                            <attribute name='record1roleid' />
            //                            <attribute name='record1id' />
            //                            <order attribute='record2id' descending='false' />
            //                            <filter type='and'>
            //                              <condition attribute='record1id' operator='in'>
            //                                {0}
            //                              </condition>
            //                            </filter>
            //                          </entity>
            //                        </fetch>";
            //            Config.AcitvityFetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true' no-lock='true' count='100'>
            //                  <entity name='activitypointer'>
            //                    <attribute name='activityid' />
            //                    <attribute name='activitytypecode' />
            //                    <attribute name='subject' alias='name'/>
            //                    <attribute name='modifiedon'/>
            //                    <attribute name='actualstart'/>
            //                    <attribute name='actualend'/>
            //                    <attribute name='statecode'/>
            //                    <attribute name='regardingobjectid'/>      
            //                    <attribute name='allparties' />
            //                       <order attribute='modifiedon' descending='true' />
            //                    <link-entity name='activityparty' from='activityid' to='activityid' alias='ab'>
            //                      <filter type='and'>
            //                        <condition attribute='partyid' operator='in'>
            //                         {0}
            //                        </condition>
            //                      </filter>
            //                    </link-entity>
            //                  </entity>
            //                </fetch>";
            //            Config.QuickViewForms = new Dictionary<string, Dictionary<string, string>>();
            //            EntitySetting account = new EntitySetting();
            //            account.DisplayName = "Accounts";
            //            account.LogicalName = "account";
            //            account.NameAttribute = "name";
            //            account.IdAttribute = "accountid";
            //            account.ParentAttributeId = "parentaccountid";
            //            account.LoadActivities = true;
            //            account.LoadConnections = true;
            //            account.Hierarchical = true;

            //            account.FetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true' no-lock='true'>
            //                  <entity name='account'>
            //                    <attribute name='accountid'/>
            //                    <attribute name='name' />
            //                    <attribute name='telephone1'/>
            //                    <attribute name='emailaddress1'/>
            //                    <attribute name='ownerid'/> 
            //                    <attribute name='parentaccountid'/>
            //                    <order attribute='name' descending='false' />
            //                      <filter type='and'>
            //                        <condition attribute='statecode' operator='eq' value='0' />
            //                       {0}
            //                      </filter>
            //                  </entity>
            //                </fetch>";
            //            JoinSetting accountContactJoin = new JoinSetting();
            //            accountContactJoin.LeftEntity = "account";
            //            accountContactJoin.RightEntity = "contact";
            //            accountContactJoin.LeftAttribute = "accountid";
            //            accountContactJoin.RightAttribute = "parentcustomerid";
            //            accountContactJoin.Name = "Get Child Contacts";
            //            account.Joins = new JoinSetting[] { accountContactJoin };

            //            Config.Entities[account.LogicalName] = account;

            //            EntitySetting contact = new EntitySetting();
            //            contact.DisplayName = "Contacts";
            //            contact.LogicalName = "contact";
            //            contact.NameAttribute = "fullname";
            //            contact.IdAttribute = "contactid";
            //            contact.ParentAttributeId = "parentcustomerid";
            //            contact.LoadActivities = true;
            //            contact.LoadConnections = true;
            //            contact.Hierarchical = false;



            //            JoinSetting parentAccountJoin = new JoinSetting();
            //            parentAccountJoin.LeftEntity = "contact";
            //            parentAccountJoin.RightEntity = "account";
            //            parentAccountJoin.LeftAttribute = "parentcustomerid";
            //            parentAccountJoin.RightAttribute = "accountid";
            //            parentAccountJoin.Name = "Get contact parent accounts";

            //            contact.Joins = new JoinSetting[] { parentAccountJoin };

            //            contact.FetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true' no-lock='true'>
            //                  <entity name='contact'>
            //                    <attribute name='contactid'/>
            //                    <attribute name='fullname' />
            //                    <attribute name='telephone1'/>
            //                    <attribute name='emailaddress1'/>
            //                    <attribute name='ownerid'/>
            //                    <attribute name='parentcustomerid'/>
            //                      <filter type='and'>
            //                        <condition attribute='statecode' operator='eq' value='0' />                    
            //                       {0}            
            //                      </filter>
            //                    <link-entity name='systemuser' from='internalemailaddress' to='emailaddress1' link-type='outer' >
            //                                <attribute name='systemuserid' alias='systemuserid' />
            //                     </link-entity>
            //                  </entity>
            //                </fetch>";


            //            Config.Entities[contact.LogicalName] = contact;



            //            // Set quick view forms
            //            Config.QuickViewForms["account"] = new Dictionary<string, string>("address1_city", "City", "telephone1", "Tel");
            //            Config.QuickViewForms["contact"] = new Dictionary<string, string>("emailaddress1", "Email", "telephone1", "Tel");
            //            Config.QuickViewForms["letter"] = new Dictionary<string, string>("modifiedon", "Modified", "statecode", "Status", "actualedend", "Due","regardingobjectid","Regarding");
            //            Config.QuickViewForms["email"] = new Dictionary<string, string>("modifiedon", "Modified", "statecode", "Status", "actualend", "Sent", "regardingobjectid", "Regarding");
            //            Config.QuickViewForms["phonecall"] = new Dictionary<string, string>("modifiedon", "Modified", "statecode", "Status", "scheduledend", "Due");
            //            Config.QuickViewForms["apppointment"] =new  Dictionary<string, string>("statecode", "Status", "scheduledstart", "Start", "scheduledend", "End", "regardingobjectid", "Regarding");
            //            Config.QuickViewForms["task"] = new Dictionary<string, string>("modifiedon", "Modified", "statecode", "Status", "scheduledend", "Due");

        }

        #endregion

        #region Commands
        [PreserveCase]
        public void CancelCommand()
        {
            CancelRequested = true;
            IsBusy.SetValue(false);
        }

        [PreserveCase]
        public void DrillIntoCommand()
        {
            Entity record = (Entity)SelectedNode.GetValue().SourceData;
            XrmForm.OpenRecordInNewWindow(record.Id.ToString(), record.LogicalName);

        }
        [PreserveCase]
        public void LoadMoreCommand()
        {
            LoadIsPaused.SetValue(false);
            Iterations.SetValue(0);
            ProcessQueue();
        }

        [PreserveCase]
        public void CloseInfoBoxCommand()
        {
            OnInfoBoxClose.Invoke(this, null);
        }

        [PreserveCase]
        public void ConnectionRoleClickCommand(NetworkViewModel that, string role)
        {

            DemoMode.SetValue(false);
            // Clear the currently selected users/teams
            SelectedUserId.SetValue(null);

            UnselectAll();

            // Find the connecitons and highlght them
            if (!ConnectionRoleIndex.ContainsKey(role))
                return;

            if (SelectedConnectionRoles.GetItems().Contains(role))
            {
                SelectedConnectionRoles.Remove(role);
                foreach (string key in ConnectionRoleIndex[role].Keys)
                {
                    SelectLink(key, false);
                }
            }
            else
            {
                SelectedConnectionRoles.Push(role);
                foreach (string key in ConnectionRoleIndex[role].Keys)
                {
                    SelectLink(key, true);
                }
            }

            if (OnSelectedLinksAdded != null)
                OnSelectedLinksAdded(this, null);
            if (OnSelectedNodesAdded != null)
                OnSelectedNodesAdded(this, null);

        }

        private void SelectLink(string key, bool select)
        {
            if (select)
            {
                HighlightedLinks.Push(key);
            }
            else
            {
                HighlightedLinks.Remove(key);
                HighlightedLinksToRemove.Push(key);
            }

            EntityLink link = linkIndex[key];
            if (link != null)
            {
                EntityNode source = link.Source;
                if (source != null)
                {
                    EntityReference sourceRef = ((Entity)source.SourceData).ToEntityReference();
                    if (select)
                    {
                        HighlightedEntities.Push(sourceRef);
                    }
                    else
                    {
                        HighlightedEntities.Remove(sourceRef);
                        HighlightedEntitiesToRemove.Push(sourceRef);
                    }
                }
                EntityNode target = link.Target;
                if (target != null)
                {
                    EntityReference targetRef = ((Entity)target.SourceData).ToEntityReference();
                    if (select)
                    {
                        HighlightedEntities.Push(targetRef);
                    }
                    else
                    {
                        HighlightedEntities.Remove(targetRef);
                        HighlightedEntitiesToRemove.Push(targetRef);
                    }
                }
            }
        }

        [PreserveCase]
        public void UserClickCommand(NetworkViewModel that, UserOrTeam user)
        {
            DemoMode.SetValue(false);
            UnselectAll();

            bool selected = false;
            // If already selected then toggle
            if (user == null || that.SelectedUserId.GetValue() == user.Id.ToString())
            {
                that.SelectedUserId.SetValue(null);
            }
            else
            {
                that.SelectedUserId.SetValue(user.Id.ToString());
                selected = true;
            }

            if (selected)
            {

                foreach (string partyid in user.Parties.Keys)
                {
                    EntityReference party = user.Parties[partyid];


                    // If overflow - add that instead
                    string key = GetIdIndexKeyEntityRef(party);

                    if (idIndex.ContainsKey(key) && idIndex[key].ReplacedByOverflow != null)
                    {

                        Entity overflow = (Entity)idIndex[GetIdIndexKeyEntityRef(party)].ReplacedByOverflow.SourceData;

                        HighlightedEntities.Push(new EntityReference(new Guid(overflow.Id), overflow.LogicalName, null));
                    }
                    else
                    {
                        HighlightedEntities.Push(party);
                    }

                }

            }
            if (OnSelectedNodesAdded != null)
                OnSelectedNodesAdded(this, null);
            if (OnSelectedLinksAdded != null)
                OnSelectedLinksAdded(this, null);
        }

        private void UnselectAll()
        {
            // Remove the selected roles
            SelectedConnectionRoles.RemoveAll();
            foreach (string link in HighlightedLinks.GetItems())
            {
                HighlightedLinksToRemove.Push(link);
            }
            HighlightedLinks.RemoveAll();

            // Remove all the Selected nodes
            foreach (EntityReference key in HighlightedEntities.GetItems())
            {
                HighlightedEntitiesToRemove.Push(key);
            }
            HighlightedEntities.RemoveAll();
        }

        [PreserveCase]
        public void ZoomInCommand()
        {
            if (OnZoom != null)
                OnZoom(this, 1);
        }

        [PreserveCase]
        public void ZoomOutCommand()
        {
            if (OnZoom != null)
                OnZoom(this, -1);
        }

        [PreserveCase]
        public void DemoModeClickCommand()
        {
            bool mode = !DemoMode.GetValue();
            DemoMode.SetValue(mode);
            if (mode)
            {
                DemoCounter = 0;
                DemoTick();
            }
        }
        #endregion

        #region Queue Processing
        private bool IsCancelRequested()
        {
            if (CancelRequested)
                IsBusy.SetValue(false);

            return CancelRequested;
        }

        private void ReportError(Exception ex)
        {
            IsBusy.SetValue(true);
            IsBusyMessage.SetValue(ex.Message);
        }

        public void ProcessQueue()
        {
            if (IsCancelRequested())
                return;
            if (!IsBusy.GetValue())
                IsBusy.SetValue(true);

            QueueIterations++;
            Trace("--------------------{0}", new object[] { QueueIterations });
            string queueString = "";
            foreach (QueuedLoad load in (QueuedLoad[])(object)Queue)
            {
                queueString += load.Entity.LogicalName;
                if (load.Join != null)
                {
                    if (load.Join.RightEntity == "connection" || load.Join.RightEntity == "activity")
                    {
                        queueString += ("[" + load.Join.RightEntity + "]");
                    }
                    else
                    {
                        queueString += ("[" + load.Join.LeftEntity + "." + load.Join.LeftAttribute + " = " + load.Join.RightEntity + "." + load.Join.RightAttribute + "]");
                    }
                }
                queueString += " | ";
            }
            Trace("Queue = {0}", new object[] { queueString });
            Trace("--------------------{0}", new object[] { QueueIterations });

            if (QueueIterations > MaxQueueItems)
            {
                PauseLoadWithMessage(ResourceStrings.PossibleInfiniteLoop);
                QueueIterations = 0;
                return;
            }

            // Pop settings from queue
            if (_currentLoad == null)
            {
                _currentLoad = Queue.Dequeue();
                if (_currentLoad == null)
                {
                    AddPendingQueuedLoads();

                    // End of load queue
                    RaiseOnNodesChanged();

                    _currentLoad = Queue.Dequeue();
                    if (_currentLoad == null)
                    {
                        IsBusy.SetValue(false);
                        Trace("End of Queue", null);
                        return;
                    }

                }
            }

            // Check if we can merge the following load
            TryToShrinkQueue();

            if (_currentLoad.Join == null)
            {

                if (!NextIteration())
                    return;
                Trace("Entity Query {0} ", new object[] { _currentLoad.Entity.LogicalName });

                SetMessage("Loading", _currentLoad.Entity.DisplayName, 0, 0);
                List<string> correctedIds = new List<string>();
                foreach (string id in _currentLoad.Ids)
                {
                    string key = GetIdIndexString(_currentLoad.Entity.LogicalName, id);
                    if (idIndexQueried.ContainsKey(key))
                    {
                        // This ID has already been loaded - so remove it from pending and don't query
                        RemovePendingID(_currentLoad.Entity.LogicalName, id);
                        continue;
                    }
                    else
                        idIndexQueried[key] = key;

                    correctedIds.Add(id);
                }

                if (correctedIds.Count > 0)
                {
                    string op = _currentLoad.Entity.Hierarchical ? "eq-above-under" : "eq";
                    string idValues = GetValues(_currentLoad.Entity.LogicalName, _currentLoad.Entity.IdAttribute, op, correctedIds);
                    string rootQuery = String.Format("\n\n<!-- " + IsBusyMessage.GetValue() + "-->\n\n" + _currentLoad.Entity.FetchXml, idValues);

                    LoadFetchXml(_currentLoad, rootQuery);
                }
                else
                {
                    _currentLoad = null;
                    CallNextProcessQueue();
                    return;
                }
            }
            else if (_currentLoad.Join.RightEntity == "activity")
            {
                _currentLoad = null;
                GetActivities();

                return;
            }
            else if (_currentLoad.Join.RightEntity == "connection")
            {
                _currentLoad = null;
                GetConnections();

                return;
            }
            else if (_currentLoad.Join != null)
            {
                Trace("Join Query {0}->{1}", new object[] { _currentLoad.Join.LeftEntity, _currentLoad.Join.RightEntity });

                // Load join
                SetMessage("Joining", _currentLoad.Entity.DisplayName, 0, 0);

                if (_currentLoad.Ids.Count > 0)
                {
                    List<string> joinIds = new List<string>();
                    foreach (string id in _currentLoad.Ids)
                    {

                        // Is the id already loaded?
                        EntityNode record = idIndex[GetIdIndexKeyEntityRef(new EntityReference(new Guid(id), _currentLoad.Join.LeftEntity, null))];
                        if (record != null)
                        {
                            object idValue = ((Entity)record.SourceData).GetAttributeValue(_currentLoad.Join.LeftAttribute);
                            if (idValue != null)
                            {
                                if (idValue.GetType() == typeof(Guid))
                                {
                                    joinIds.Add(((Guid)idValue).Value);
                                }
                                else if (idValue.GetType() == typeof(EntityReference))
                                {
                                    joinIds.Add(((EntityReference)idValue).Id.Value);
                                }
                            }
                        }
                    }
                    if (joinIds.Count > 0)
                    {
                        Trace("Found {0} Join IDs", new object[] { joinIds.Count });
                        JoinSetting join = _currentLoad.Join;

                        // Check if the key is the primary key - if so we can use the hierarchical query
                        string op = (_currentLoad.Entity.Hierarchical
                            && join.RightAttribute == _currentLoad.Entity.IdAttribute) ? "eq-above-under" : "eq";

                        // Remove any ids already joined or exlcuded - this removes infinite loops in joins
                        List<string> joinIdsCorrected = new List<string>();
                        if (join.ExcludeIds == null)
                        {
                            join.ExcludeIds = new Dictionary<string, string>();
                        }

                        foreach (string id in joinIds)
                        {
                            if (!join.ExcludeIds.ContainsKey("ID" + id))
                            {
                                joinIdsCorrected.Add(id);
                                join.ExcludeIds["ID" + id] = id;

                            }

                        }


                        if (joinIdsCorrected.Count > 0)
                        {
                            Trace("Correted Join IDS {0}", new object[] { joinIdsCorrected.Count });


                            string joinXml = string.Format(
                                "\n\n<!-- " + IsBusyMessage.GetValue() + "-->\n\n" + _currentLoad.Entity.FetchXml,
                                GetValues(_currentLoad.Entity.LogicalName, _currentLoad.Join.RightAttribute, op, joinIdsCorrected));

                            LoadFetchXml(_currentLoad, joinXml);
                        }
                        else
                        {
                            Trace("Join supressed due to infinite loop possibility", null);
                            _currentLoad = null;
                            CallNextProcessQueue();
                        }
                        return;
                    }
                    else
                    {
                        Trace("Nothing to Load", null);
                        // Nothing to load
                        _currentLoad = null;
                        CallNextProcessQueue();
                        return;
                    }
                }
                else
                {
                    CallNextProcessQueue();
                    return;
                }

            }

        }

        private void TryToShrinkQueue()
        {
            QueuedLoad nextLoad = Queue.Peek();

            if (nextLoad != null && (nextLoad.Entity.LogicalName == _currentLoad.Entity.LogicalName))
            {
                string thisJoin = _currentLoad.Join != null ? _currentLoad.Join.RightEntity : null;
                string nextJoin = nextLoad.Join != null ? nextLoad.Join.RightEntity : null;
                if (thisJoin == nextJoin)
                {
                    Trace("Next Load is same join", null);
                    QueuedLoad load = Queue.Dequeue();
                    _currentLoad.Ids.AddRange(load.Ids);
                }
            }
        }

        private void AddPendingQueuedLoads()
        {
            foreach (string pendingEntity in pendingLinks.Keys)
            {
                Trace("Pending Entity {0}", new object[] { pendingEntity });
                EntitySetting entity = Config.Entities[pendingEntity];
                if (entity != null && pendingLinks[pendingEntity].Count > 0)
                {
                    Queue.Enqueue(new QueuedLoad((List<string>)(object)pendingLinks[pendingEntity].Keys, entity, null));
                    Trace("Queued {0}", new object[] { pendingEntity });
                }
                else
                {
                    Trace("Nothing to Load", null);
                }
            }
        }

        private void CallNextProcessQueue()
        {
            if (_lastInterrupt == null)
                _lastInterrupt = Date.Now;

            if ((_lastInterrupt - Date.Now) > 3000)
            {
                _lastInterrupt = Date.Now;
                Window.SetTimeout(delegate()
                {
                    ProcessQueue();
                }, 100);
            }
            else
            {
                ProcessQueue();
            }
        }
        private bool NextIteration()
        {
            int i = Iterations.GetValue();
            i++;

            Iterations.SetValue(i);
            if (i < Config.IterationCountPerLoad)
            {
                return true;
            }
            else
            {
                PauseLoadWithMessage(ResourceStrings.RecordLimitExceeded);
                return false;
            }
        }

        private void PauseLoadWithMessage(string message)
        {
            IsBusyMessage.SetValue(message);
            LoadIsPaused.SetValue(true);
            RaiseOnNodesChanged();
        }

        private void Trace(string message, object[] values)
        {
            // We don't want the overhead of doing the string format if we are not tracing
            // so we have to manually do the spliting of parameter arrays
            if (Config.Trace)
            {
                object value1 = values != null && values.Length > 0 ? values[0] : null;
                object value2 = values != null && values.Length > 1 ? values[1] : null;
                object value3 = values != null && values.Length > 2 ? values[2] : null;
                object value4 = values != null && values.Length > 3 ? values[3] : null;
                object value5 = values != null && values.Length > 4 ? values[4] : null;
                string trace = String.Format(message, value1, value2, value3, value4);
                Script.Literal("ss.Debug.writeln({0})", trace); // This is so that we alway emmit the log even when not compiled in debug
            }
        }

        private void SetMessage(string status, string entity, float i, float total)
        {
            // Post a message to the UI
            string message = String.Format(ResourceStrings.StatusMessage, status, entity, Nodes.Count);
            IsBusyMessage.SetValue(message);
            IsBusyProgress.SetValue((i / total) * 100);

        }
        #endregion

        #region Fetch Queries
        private EntityNode GetParentEntity(Entity record, string parentAttributeName, string parentEnityLogicalName)
        {

            object parent = record.GetAttributeValue(parentAttributeName);
            string parentid = "";
            if (parent.GetType() == typeof(Guid))
            {
                parentid = ((Guid)parent).Value;

            }
            else if (parent.GetType() == typeof(EntityReference))
            {
                parentid = ((EntityReference)parent).Id.Value;
                parentEnityLogicalName = ((EntityReference)parent).LogicalName;

            }

            EntityNode parentNode = null;

            if (parent != null)
            {
                string key = parentEnityLogicalName + parentid;
                if (idIndex.ContainsKey(key))
                {
                    parentNode = idIndex[key];
                }
            }
            return parentNode;
        }
        private void LoadFetchXml(QueuedLoad load, string rootQuery)
        {
            // Get all the related records to the root 
            SetMessage(ResourceStrings.Querying, load.Entity.DisplayName, 0, 0);
            OrganizationServiceProxy.BeginRetrieveMultiple(rootQuery, delegate(object state)
            {
                try
                {
                    EntityCollection rootResults = OrganizationServiceProxy.EndRetrieveMultiple(state, typeof(Entity));

                    Trace("Query for {0} returned {1} records", new object[] { load.Entity.LogicalName, rootResults.Entities.Count });
                    ProcessQueryResults(load, rootResults);

                }
                catch (Exception ex)
                {
                    ReportError(ex);
                    return;
                }
            });
        }

        private void ProcessQueryResults(QueuedLoad load, EntityCollection rootResults)
        {
            int total = rootResults.Entities.Count;
            int index = 0;
            List<string> ids = new List<string>();

            foreach (Entity record in rootResults.Entities)
            {
                if (IsCancelRequested())
                    return;
                SetMessage(ResourceStrings.Processing, load.Entity.DisplayName, index, total);

                // Check if this is a pending load - and remove it
                RemovePendingID(record.LogicalName, record.Id);

                index++;
                ids.Add(record.Id);
                string name = record.GetAttributeValueString(load.Entity.NameAttribute == null ? "name" : load.Entity.NameAttribute);
                // If this is the root entity then set the window title
                bool rootNode = RootEntityId.Value == record.Id.ToLowerCase();

                if (rootNode)
                {
                    Window.Document.Title = name;
                }

                if (!IsAlsoAUser(record) && !IndexContainsEntity(record))
                {
                    EntityNode newNode = new EntityNode(name, 100);
                    newNode.Root = rootNode;
                    newNode.X = _startX;
                    newNode.Y = _startY;
                    newNode.SourceData = record;
                    if (!AddEntity(record, newNode, load.Entity, false))
                        return;

                    // Add User reference for owner
                    EntityReference owner = record.GetAttributeValueEntityReference("ownerid");
                    if (owner != null)
                    {
                        UserOrTeam user = GetUserOrTeamReference(owner);
                        user.Parties[record.Id.ToString()] = record.ToEntityReference();
                    }
                }
            }

            Trace("Linking to Parents {0} {1} records", new object[] { load.Entity.LogicalName, rootResults.Entities.Count });
            index = 0;
            // Add the parent links
            foreach (Entity record in rootResults.Entities)
            {
                if (IsCancelRequested())
                    return;
                SetMessage(ResourceStrings.Linking, load.Entity.DisplayName, index, total);



                EntityNode thisNode = GetEntity(record);

                if (thisNode == null)
                    continue;
                // Add the heiarchical links
                EntityNode parentNode = GetParentEntity(record, load.Entity.ParentAttributeId, load.Entity.LogicalName);

                if (parentNode != null)
                {
                    // Create Link
                    AddLink(thisNode, parentNode, false);
                }



                // Add the backlinks
                // e.g. if this is a contact then back link via the parentcustomerid field
                if (load.Join != null)
                {
                    Trace("Adding backlinks {0}->{1}", new object[] { load.Entity.LogicalName, load.Join.RightEntity });
                    EntityNode joinedNode = GetParentEntity(record, load.Join.RightAttribute, load.Join.RightEntity);
                    if (joinedNode != null)
                    {
                        AddLink(thisNode, joinedNode, false);
                    }
                    else
                    {

                        // Add pending link

                    }
                }
            }

            if (ids.Count > 0)
            {

                // add links
                if (load.Entity.Joins != null)
                {
                    Trace("Adding Joins to {0}", new object[] { ids.Count });
                    foreach (JoinSetting join in load.Entity.Joins)
                    {
                        EntitySetting joinedTo = Config.Entities[join.RightEntity];
                        if (joinedTo != null)
                        {
                            Trace("Queing Join  {0}.{1} -> {2}.{3} {4}", new object[] { join.LeftEntity, join.LeftAttribute, join.RightEntity, join.RightAttribute, join.Name });
                            Queue.Enqueue(new QueuedLoad(ids, joinedTo, join));
                        }
                    }
                }

                if (load.Entity.LoadActivities)
                {
                    Queue.Enqueue(new QueuedLoad(ids, load.Entity, NewJoinSetting(load.Entity.LogicalName, "activity")));
                }

                if (load.Entity.LoadConnections)
                {
                    Queue.Enqueue(new QueuedLoad(ids, load.Entity, NewJoinSetting(load.Entity.LogicalName, "connection")));
                }
            }
            _currentLoad = null;
            CallNextProcessQueue();

        }
        private string NormalisedGuid(string guid)
        {
            if (guid.Substr(0, 1) == "{")
                guid = guid.Substr(1,36);

            return guid.ToLowerCase();

        }
        private string GetValues(string logicalName, string attribute, string op, List<string> ids)
        {
            string values = "<filter type='or'>";
            if (op == "eq-above-under")
            {
                foreach (string id in ids)
                {
                    values += string.Format("<condition attribute='{0}' operator='{2}' value='{1}'/>", attribute, id, "eq-or-above");
                    values += string.Format("<condition attribute='{0}' operator='{2}' value='{1}'/>", attribute, id, "under");
                }
            }
            else
            {
                foreach (string id in ids)
                {
                    // Check if we've loaded the record yet                
                    values += string.Format("<condition attribute='{0}' operator='{2}' value='{1}'/>", attribute, id, op);
                }
            }
            values += "</filter>";
            return values;
        }
        #endregion

        #region Activities
        private void GetActivities()
        {

            if (IsCancelRequested())
                return;
            SetMessage(ResourceStrings.Loading, ResourceStrings.Activities, 0, 0);

            string parties = "";

            foreach (string key in this.idIndexActivityLoad.Keys)
            {
                parties += "<value>" + ((Entity)idIndexActivityLoad[key].SourceData).Id + "</value>";
            }
            this.idIndexActivityLoad.Clear();
            if (parties != "")
            {
                string fetchXml = string.Format(Config.AcitvityFetchXml, parties);

                OrganizationServiceProxy.BeginRetrieveMultiple(fetchXml, delegate(object state2)
                {

                    if (IsCancelRequested())
                        return;
                    EntityCollection rootResults2;
                    int total = 0;

                    try
                    {
                        rootResults2 = OrganizationServiceProxy.EndRetrieveMultiple(state2, typeof(Entity));
                        total = rootResults2.Entities.Count;
                    }
                    catch (Exception ex)
                    {
                        ReportError(ex);
                        return;
                    }

                    DelegateItterator.CallbackItterate(delegate(int index, Action nextCallBack, ErrorCallBack errorCallBack)
                    {
                        if (IsCancelRequested())
                            return;
                        ProcessActivity(rootResults2, index, total);
                        nextCallBack();
                    }, total,
                    delegate()
                    {

                        // Add in pending links
                        // Load the accounts referenced by activity parties - both directly and directly via other contacts
                        // For each account - provide a link to exapand further
                        LoadUsers(delegate()
                        {

                            _currentLoad = null;
                            CallNextProcessQueue();
                            return;
                        });
                    },
                    delegate(Exception ex) { });
                });
            }
            else
            {

                _currentLoad = null;
                CallNextProcessQueue();
                return;
            }
        }

        private void ProcessActivity(EntityCollection rootResults2, int index, int total)
        {
            Entity record = rootResults2.Entities[index];

            SetMessage(ResourceStrings.Processing, ResourceStrings.Activities, index, total);
            EntityNode newNode;
            record.LogicalName = record.GetAttributeValueString("activitytypecode");

            bool alreadyAdded = false;
            if (!IndexContainsEntity(record))
            {

                newNode = new EntityNode(record.GetAttributeValueString("name"), 100);
                newNode.IsActivity = true;
                newNode.X = _startX;
                newNode.Y = _startY;
                newNode.SourceData = record;

                if (!AddEntity(record, newNode, null, true))
                    return;
            }
            else
            {
                newNode = GetEntity(record);
                alreadyAdded = true;
            }


            // Go through the activity parties and link to the records
            EntityCollection allParties = (EntityCollection)record.GetAttributeValue("allparties");
            int i = 0;
            bool overflow = false;

            foreach (Entity item in allParties.Entities)
            {
                if (IsCancelRequested())
                    return;
                if ((index % 20) == 0)
                {
                    SetMessage(ResourceStrings.Processing, ResourceStrings.ActivityLinks, index, total);
                }
                i++;

                ActivityParty party = (ActivityParty)item;
                EntityReference partyid = party.PartyId;
                if (partyid != null) // Only load resolved parties
                {
                    //Trace(String.Format("Party {0} {1} {2}",partyid.LogicalName, partyid.Name,partyid.Id));
                    // Do we have it?
                    // We must have at least one since the activity has been loaded in the first place!
                    EntityLink linkToExisting = AddLinkIfLoaded(newNode, partyid, true);

                    if (linkToExisting == null)
                    {

                        // Any records not loaded - add a pending link
                        if (party.PartyId.LogicalName == "systemuser" || party.PartyId.LogicalName == "team")
                        {

                            party.ActivityID.LogicalName = ((Entity)newNode.SourceData).LogicalName;
                            UserOrTeam user = GetUserOrTeamReference(party.PartyId);

                            user.Parties[party.ActivityID.Id.ToString()] = party.ActivityID;
                        }
                        else
                        {

                            AddPendingLink(newNode, partyid);
                        }
                    }
                    else
                    {
                        if (newNode.ParentNode == null)
                        {
                            // Allocate each entity a single parent - which is the first found party

                            newNode.ParentNode = linkToExisting.Target;
                            newNode.ParentNode.ActivityCount++;
                        }

                        // Check the count of activities 

                        bool overflowAlreadyAdded = false;

                        bool overflowed = newNode.ParentNode.ActivityCount > OverflowMax && newNode.ParentNode == linkToExisting.Target;

                        // If the linked to source activity is already load then don't overflow it
                        if (alreadyAdded)
                        {

                            overflow = false;
                        }

                        if (overflowed)
                        {


                            if (newNode.ParentNode.OverflowNode == null)
                            {

                                // Create overflow node if not already
                                EntityNode overflowNode = new EntityNode(ResourceStrings.DoubleClickToExpand, 1);
                                overflowNode.Id = "overflow" + partyid.Id.Value;
                                overflowNode.ParentNode = newNode.ParentNode;
                                overflowNode.X = _startX;
                                overflowNode.Y = _startY;
                                Entity overflowEntity = new Entity("overflow");
                                overflowEntity.Id = "overflow" + ((Entity)newNode.SourceData).Id;
                                overflowNode.SourceData = overflowEntity;

                                Nodes.Add(overflowNode);
                                newNode.ParentNode.OverflowNode = overflowNode;
                                newNode.ReplacedByOverflow = overflowNode;

                            }
                            else if (linkToExisting.Source.ParentNode == linkToExisting.Target && !alreadyAdded)
                            {
                                overflowAlreadyAdded = true;
                                linkToExisting.Source.ReplacedByOverflow = linkToExisting.Target.OverflowNode;

                            }

                            if (newNode.ReplacedByOverflow != null)
                            {
                                if (newNode.ReplacedByOverflow.Children == null)
                                    newNode.ReplacedByOverflow.Children = new List<EntityNode>();
                                if (!newNode.ReplacedByOverflow.Children.Contains(newNode))
                                    newNode.ReplacedByOverflow.Children.Add(newNode);
                            }

                        }

                        if (!overflowAlreadyAdded)
                        {
                            if (linkToExisting.Source.ReplacedByOverflow != null)
                            {
                                linkToExisting.Source = linkToExisting.Source.ReplacedByOverflow;
                                linkToExisting.Source.Links.Add(linkToExisting);
                                if (!linkToExisting.Id.StartsWith("overflow"))
                                    linkToExisting.Id = "overflow" + linkToExisting.Id;
                            }

                            if (!linkIndex.ContainsKey(linkToExisting.Id))
                            {
                                linkIndex[linkToExisting.Id] = linkToExisting;

                                Links.Add(linkToExisting);
                            }
                            else
                            {

                            }
                        }

                    }

                }
            }
            if (newNode.ReplacedByOverflow == null)
            {
                if (!alreadyAdded)
                {
                    Nodes.Add(newNode);
                }
            }

        }
        #endregion

        #region Connections
        private void GetConnections()
        {
            if (IsCancelRequested())
                return;

            SetMessage(ResourceStrings.Loading, ResourceStrings.Connections, 0, 0);

            string values = "";
            string names = "";
            foreach (string key in this.idIndexConnectionLoad.Keys)
            {
                values += "<value>" + ((Entity)idIndexConnectionLoad[key].SourceData).Id + "</value>";
                names += idIndexConnectionLoad[key].Name + " , ";
            }

            this.idIndexConnectionLoad.Clear();
            if (values != "")
            {
                // Clear out the parties so we don't load again
                string fetchXml = string.Format(Config.ConnectionFetchXml, values);

                OrganizationServiceProxy.BeginRetrieveMultiple(fetchXml, delegate(object state2)
                {

                    if (IsCancelRequested())
                        return;

                    EntityCollection rootResults2 = null;
                    int total = 0;
                    try
                    {
                        rootResults2 = OrganizationServiceProxy.EndRetrieveMultiple(state2, typeof(Connection));
                        total = rootResults2.Entities.Count;
                    }
                    catch (Exception ex)
                    {
                        ReportError(ex);
                        return;
                    }

                    DelegateItterator.CallbackItterate(delegate(int index, Action nextCallBack, ErrorCallBack errorCallBack)
                    {
                        if (IsCancelRequested())
                            return;

                        ProcessConnection(rootResults2, index, total);
                        nextCallBack();

                    }, total,
                    delegate()
                    {
                        _currentLoad = null;
                        CallNextProcessQueue();
                        return;

                    },
                    delegate(Exception ex) { });
                });
            }
            else
            {
                _currentLoad = null;
                CallNextProcessQueue();
                return;
            }
        }


        private void ProcessConnection(EntityCollection rootResults2, int index, int total)
        {

            Connection record = (Connection)rootResults2.Entities[index];

            SetMessage(ResourceStrings.Processing, ResourceStrings.Connections, index, total);

            EntityReference record1id = record.Record1Id; // we have this already
            EntityReference record2id = record.Record2Id;

            // Get the roles
            EntityReference role1 = record.Record1RoleId;
            EntityReference role2 = record.Record2RoleId;


            EntityNode linkFrom = GetEntityFromReference(record1id);
            EntityLink linkToExisting = AddLinkIfLoaded(linkFrom, record2id, false);

            bool record1UserOrTeam = record1id.LogicalName == "systemuser" || record1id.LogicalName == "team";
            bool record2UserOrTeam = record2id.LogicalName == "systemuser" || record2id.LogicalName == "team";

            if (!record1UserOrTeam && !record2UserOrTeam && linkToExisting != null)
            {
                // Add the the connection role index
                if (role1 != null)
                {
                    if (!ConnectionRoleIndex.ContainsKey(role1.Name))
                    {
                        ConnectionRoleIndex[role1.Name] = new Dictionary<string, EntityLink>();
                    }

                    if (!ConnectionRoleIndex[role1.Name].ContainsKey(linkToExisting.Id))
                    {
                        // Add the connection
                        ConnectionRoleIndex[role1.Name][linkToExisting.Id] = linkToExisting;
                    }
                    if (!ConnectionRoles.GetItems().Contains(role1.Name))
                    {
                        ConnectionRoles.Push(role1.Name);
                    }
                }

                if (role2 != null)
                {
                    if (!ConnectionRoleIndex.ContainsKey(role2.Name))
                    {
                        ConnectionRoleIndex[role2.Name] = new Dictionary<string, EntityLink>();
                    }
                    // Add the connection

                    if (!ConnectionRoleIndex[role2.Name].ContainsKey(linkToExisting.Id))
                    {
                        // Add the connection
                        ConnectionRoleIndex[role2.Name][linkToExisting.Id] = linkToExisting;
                    }
                    if (!ConnectionRoles.GetItems().Contains(role2.Name))
                    {
                        ConnectionRoles.Push(role2.Name);
                    }
                }

            }

            if (linkToExisting == null)
            {

                // Any records not loaded - add a pending link
                if (record2UserOrTeam)
                {
                    UserOrTeam user = GetUserOrTeamReference(record2id);
                    user.Parties[record2id.Id.ToString()] = record1id;
                }
                else if (record1UserOrTeam)
                {
                    UserOrTeam user = GetUserOrTeamReference(record1id);
                    user.Parties[record1id.Id.ToString()] = record2id;
                }
                else
                {
                    //Trace(String.Format("Pending Connections to {0} -> {1}", linkFrom.Name, record2id.Name));
                    AddPendingLink(linkFrom, record2id);
                }
            }
        }
        #endregion

        #region UserTeam Management

        private bool IsAlsoAUserFromEntityReference(EntityReference record)
        {
            Entity entityRecord = new Entity(record.LogicalName);
            entityRecord.Id = record.Id.Value;
            return IsAlsoAUser(entityRecord);
        }

        private bool IsAlsoAUser(Entity record)
        {

            if (userContacts.ContainsKey(record.Id))
                return true;
            else
            {
                if ((record.LogicalName == "contact" && record.GetAttributeValue("systemuserid") != null))
                {
                    userContacts[record.Id] = record;
                    return true;
                }

                if (record.LogicalName == "contact")
                {
                    // Check the email domain
                    string emailAddress = record.GetAttributeValueString("emailaddress1");
                    if (emailAddress != null)
                    {
                        string domain = GetEmailDomain(emailAddress);
                        return EmailDomains.ContainsKey(domain);
                    }
                }
            }
            return false;
        }

        public UserOrTeam GetUserOrTeamReference(EntityReference userOrTeamId)
        {
            UserOrTeam user;
            // Add to the list of users if not already
            if (!userOrTeamIds.ContainsKey(userOrTeamId.Id.ToString()))
            {

                user = new UserOrTeam();
                user.Id = userOrTeamId.Id.ToString();
                user.IsTeam = (userOrTeamId.LogicalName == "team");
                user.LogicalName = user.IsTeam ? "team" : "systemuser";
                user.FullName = null;
                user.Parties = new Dictionary<string, EntityReference>();
                userOrTeamIds[user.Id] = user;


            }
            else
            {
                user = userOrTeamIds[userOrTeamId.Id.ToString()];
            }
            return user;
        }



        private void LoadUsers(Action callback)
        {
            // Load any users that are not loaded
            string useridValues = "";
            string teamidValues = "";
            foreach (string userid in userOrTeamIds.Keys)
            {
                UserOrTeam userOrTeam = userOrTeamIds[userid];
                if (userOrTeam.FullName == null)
                {
                    if (userOrTeam.IsTeam)
                    {
                        teamidValues += "<value>" + userOrTeam.Id + "</value>";

                    }
                    else
                    {
                        useridValues += "<value>" + userOrTeam.Id + "</value>";
                    }

                }
            }

            int count = 1;
            int complete = 0;
            if (useridValues.Length == 0 && teamidValues.Length == 0)
            {
                callback();
                return;
            }

            count = (useridValues.Length > 0 && teamidValues.Length > 0) ? 2 : 1;

            string userQuery = String.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false' nolock='true'>
                              <entity name='systemuser'>
                                <attribute name='fullname' />
                                <attribute name='systemuserid' />
                                <attribute name='internalemailaddress'/>
                                <filter type='and'>
                                  <condition attribute='systemuserid' operator='in' >{0}</condition>
                                </filter>
                              </entity>
                            </fetch>", useridValues);
            string teamQuery = String.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false' nolock='true'>
                              <entity name='team'>
                                <attribute name='name' />
                                <attribute name='teamid' />
                                <filter type='and'>
                                  <condition attribute='teamid' operator='in' >{0}</condition>
                                </filter>
                              </entity>
                            </fetch>", teamidValues);
            if (useridValues.Length > 0)
            {
                OrganizationServiceProxy.BeginRetrieveMultiple(userQuery, delegate(object state)
                        {
                            if (IsCancelRequested())
                                return;

                            EntityCollection users = OrganizationServiceProxy.EndRetrieveMultiple(state, typeof(UserOrTeam));
                            foreach (UserOrTeam user in users.Entities)
                            {
                                if (IsCancelRequested())
                                    return;

                                userOrTeamIds[user.Id].FullName = user.FullName;
                                user.Parties = userOrTeamIds[user.Id].Parties;
                                if (user.InternalEmailAddress != null)
                                {
                                    string domain = GetEmailDomain(user.InternalEmailAddress);

                                    if (!EmailDomains.ContainsKey(domain))
                                    {
                                        EmailDomains[domain] = domain;
                                    }
                                }
                                // Add the users to the list
                                this.UsersAndTeams.Push(user);
                            }
                            complete++;
                            if (complete >= count)
                                callback();
                        });
            }
            if (teamidValues.Length > 0)
            {
                OrganizationServiceProxy.BeginRetrieveMultiple(teamQuery, delegate(object state)
                {
                    if (IsCancelRequested())
                        return;

                    EntityCollection teams = OrganizationServiceProxy.EndRetrieveMultiple(state, typeof(UserOrTeam));
                    foreach (UserOrTeam team in teams.Entities)
                    {
                        if (IsCancelRequested())
                            return;
                        team.FullName = team.Name;
                        userOrTeamIds[team.Id].FullName = team.Name;

                        team.IsTeam = true;
                        team.Parties = userOrTeamIds[team.Id].Parties;
                        // Add the users to the list
                        this.UsersAndTeams.Push(team);
                    }
                    complete++;
                    if (complete >= count)
                        callback();
                });
            }
        }

        private static string GetEmailDomain(string emailAddress)
        {
            string domain = null;
            string[] emailParts = emailAddress.Split('@');
            if (emailParts.Length > 1)
            {
                domain = emailParts[1];

            }
            return domain;
        }



        #endregion

        #region Link/Join Management

        private JoinSetting NewJoinSetting(string fromEntity, string joinEntity)
        {
            JoinSetting join = new JoinSetting();
            join.LeftEntity = fromEntity;
            join.RightEntity = joinEntity;
            return join;
        }

        private string GetLinkId(string fromId, string toId)
        {
            // Always put the 'least' guid frist
            string key;
            if (fromId.CompareTo(toId) > 0)
            {
                key = fromId.Replace("-", "") + "_" + toId.Replace("-", "");
            }
            else
            {
                key = toId.Replace("-", "") + "_" + fromId.Replace("-", "");
            }

            return "L" + key;
        }

        private void RemovePendingID(string logicalName, string id)
        {

            if (pendingLinks.ContainsKey(logicalName))
            {
                Dictionary<string, PendingLink> linkIds = pendingLinks[logicalName];

                if (linkIds.ContainsKey(id))
                {
                    PendingLink link = linkIds[id];
                    linkIds.Remove(id);
                }
            }
        }
        /// <summary>
        /// Adds a link from one entity to another
        /// </summary>
        /// <param name="delayAdd">creates the link but doesn't add it to the model yet</param>
        /// <returns></returns>
        private EntityLink AddLink(EntityNode target, EntityNode source, bool delayAdd)
        {
            Entity sourceEntity = (Entity)source.SourceData;
            Entity targetEntity = (Entity)target.SourceData;
            if ((target.LinkedToIds != null) && (source.LinkedToIds != null))
            {
                if (target.LinkedToIds.ContainsKey(sourceEntity.Id) && source.LinkedToIds.ContainsKey(targetEntity.Id))
                {
                    // return the current link
                    return target.LinkedToIds[sourceEntity.Id].Link;
                }
            }
            EntityLink link = new EntityLink();

            link.Source = source;
            link.Target = target;
            SetLinkedIds(link.Target, link.Source, sourceEntity, targetEntity, link);

            // If either side has an overflow - then set it to that instead
            if (!source.IsActivity && link.Source.ParentNode != null && link.Source.ParentNode.OverflowNode != null)
            {
                link.Source = link.Source.ParentNode.OverflowNode;
                link.Source.Links.Add(link);
            }
            else if (source.IsActivity && link.Source.OverflowNode != null)
            {
                link.Source = link.Source.OverflowNode;
                link.Source.Links.Add(link);
            }

            if (!target.IsActivity && link.Target.ParentNode != null && link.Target.ParentNode.OverflowNode != null)
            {
                link.Target = link.Target.ParentNode.OverflowNode;
            }
            else if (target.IsActivity && link.Target.OverflowNode != null)
            {
                link.Target = link.Target.OverflowNode;
                link.Target.Links.Add(link);
            }

            link.Id = GetLinkId(sourceEntity.Id, targetEntity.Id);

            if (!linkIndex.ContainsKey(link.Id))
            {
                if (!delayAdd)
                {
                    linkIndex[link.Id] = link;
                    Links.Add(link);
                }
            }
            return link;
        }

        private PendingLink AddPendingLink(EntityNode newNode, EntityReference pendingEntity)
        {
            Trace("Add Pending Link {0} {1} -> {2} {3}", new object[] { ((Entity)newNode.SourceData).LogicalName, newNode.Name, pendingEntity.LogicalName, pendingEntity.Id });
            PendingLink pending = new PendingLink();
            pending.Source = newNode;
            pending.Target = pendingEntity;

            if (!pendingLinks.ContainsKey(pendingEntity.LogicalName))
            {
                pendingLinks[pendingEntity.LogicalName] = new Dictionary<string, PendingLink>();

            }
            if (!IsAlsoAUserFromEntityReference(pendingEntity))
            {
                pendingLinks[pendingEntity.LogicalName][pendingEntity.Id.ToString()] = pending;

            }

            return pending;
        }

        private EntityLink AddLinkIfLoaded(EntityNode target, EntityReference source, bool delayAdd)
        {
            EntityNode sourceNode = GetEntityFromReference(source);
            if (sourceNode != null)
            {
                // Create Link
                EntityLink link = AddLink(sourceNode, target, delayAdd);
                return link;
            }
            return null;
        }

        private static void SetLinkedIds(EntityNode target, EntityNode source, Entity sourceEntity, Entity targetEntity, EntityLink link)
        {
            if (source.LinkedToIds == null) source.LinkedToIds = new Dictionary<string, EntityNodeLink>();
            if (target.LinkedToIds == null) target.LinkedToIds = new Dictionary<string, EntityNodeLink>();
            source.LinkedToIds[targetEntity.Id] = new EntityNodeLink(target, link, 1);
            target.LinkedToIds[sourceEntity.Id] = new EntityNodeLink(source, link, 1);
        }

        private bool AddEntity(Entity record, EntityNode newNode, EntitySetting settings, bool delayAdd)
        {
            if (((Nodes.Count + 1) % 40) == 0)
            {
                // Zoom out the more nodes there are - maybe better to use the extent of the layout...
                OnZoom.Invoke(this, -1);
            }
            string key = GetIdIndexKey(record);
            idIndex[key] = newNode;

            if (settings != null)
            {
                if (!newNode.LoadedActivities && settings.LoadActivities)
                {
                    idIndexActivityLoad[key] = newNode;
                    newNode.LoadedActivities = true;
                }
                if (!newNode.LoadedConnections && settings.LoadConnections)
                {
                    idIndexConnectionLoad[key] = newNode;
                    newNode.LoadedConnections = true;
                }
            }

            if (!delayAdd)
            {
                Nodes.Add(newNode);
            }

            return true;
        }
        private string GetIdIndexKey(Entity record)
        {
            return record.LogicalName + record.Id;
        }
        private string GetIdIndexKeyEntityRef(EntityReference record)
        {
            return record.LogicalName + record.Id.ToString();
        }
        private string GetIdIndexString(string logicalName, string id)
        {
            return logicalName + id;
        }
        private bool IndexContainsEntity(Entity record)
        {
            return idIndex.ContainsKey(GetIdIndexKey(record));
        }
        private bool IndexContainsEntityReference(EntityReference record)
        {
            return idIndex.ContainsKey(GetIdIndexKeyEntityRef(record));
        }
        private EntityNode GetEntity(Entity record)
        {
            return idIndex[GetIdIndexKey(record)];
        }
        private EntityNode GetEntityFromReference(EntityReference record)
        {
            return idIndex[GetIdIndexKeyEntityRef(record)];
        }
        public void RaiseOnNodesChanged()
        {
            if (!CancelRequested)
            {
                if (OnNewNodes != null)
                    OnNewNodes(this, null);
            }
        }
        public void RaiseOnNodesChangedWithNoFastForward()
        {
            if (!CancelRequested)
            {
                if (OnNewNodes != null)
                    OnNewNodes(this, new EventArgs());
            }
        }

        public void ExpandOverflow(EntityNode entityNode)
        {

            // Clear the selected user
            SelectedUserId.SetValue(null);
            SelectedConnectionRoles.RemoveAll();
            UnselectAll();
            OnSelectedNodesAdded(this, null);
            OnSelectedLinksAdded(this, null);

            if (entityNode.Children != null)
            {
                // Add any overflow nodes
                int expandedCount = 0;
                foreach (EntityNode node in entityNode.Children)
                {
                    node.X = entityNode.X;
                    node.Y = entityNode.Y;
                    node.Fixed = false;
                    if (!Nodes.Contains(node))
                        Nodes.Add(node);
                    node.ReplacedByOverflow = null;
                    expandedCount++;

                    // Add the links
                    foreach (KeyValuePair<string, EntityNodeLink> linkedTo in node.LinkedToIds)
                    {
                        EntityLink link = new EntityLink(); ;
                        link.Source = node;
                        link.Target = linkedTo.Value.Target;
                        link.Id = GetLinkId(((Entity)node.SourceData).Id, ((Entity)link.Target.SourceData).Id);

                        if (linkIndex.ContainsKey(link.Id))
                        {
                            link = linkIndex[link.Id];

                        }
                        else
                        {
                            linkIndex[link.Id] = link;
                            Links.Add(link);
                        }

                    }
                    if (expandedCount == OverflowMax && entityNode.Children.Count > (OverflowMax + 1))
                    {
                        break;
                    }
                }
                entityNode.Children.RemoveRange(0, expandedCount);
                entityNode.ParentNode.ActivityCount = entityNode.Children.Count;

                if (entityNode.Children.Count == 0)
                {
                    entityNode.Children = null;
                    // Remove the overflow links and node
                    Nodes.Remove(entityNode);
                    foreach (EntityLink link in entityNode.Links)
                    {
                        linkIndex.Remove(link.Id);
                        Links.Remove(link);
                    }
                }

                RaiseOnNodesChangedWithNoFastForward();

            }
        }

        private void DemoTick()
        {
            if (!DemoMode.GetValue())
                return;

            int userCount = UsersAndTeams.GetItems().Length;
            int connectionCount = ConnectionRoles.GetItems().Length;



            int connectionIndex = (DemoCounter - userCount);
            if (connectionIndex >= ConnectionRoles.GetItems().Length)
            {
                DemoCounter = 0;
                UserClickCommand(this, null);

            }
            else if (DemoCounter < userCount)
            {
                UserOrTeam[] users = UsersAndTeams.GetItems();
                // Select User
                string userId = users[DemoCounter].Id;

                UserClickCommand(this, userOrTeamIds[userId]);

                DemoCounter++;
            }
            else if (connectionIndex >= 0 && connectionIndex < connectionCount)
            {
                string[] roles = ConnectionRoles.GetItems();
                string role = roles[connectionIndex];

                ConnectionRoleClickCommand(this, role);

                DemoCounter++;
            }
            DemoMode.SetValue(true);

            // Schedule another demo tick if we are still in demo mode
            if (DemoMode.GetValue())
            {
                Window.SetTimeout(DemoTick, Config.DemoTickLength != null && Config.DemoTickLength > 500 ? Config.DemoTickLength : 2000);
            }
        }
        #endregion
    }
}
