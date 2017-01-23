//! ClientUI.debug.js
//

(function($){

Type.registerNamespace('ClientUI.D3Api');

Type.registerNamespace('ClientUI');

////////////////////////////////////////////////////////////////////////////////
// ClientUI.ResourceStrings

ClientUI.ResourceStrings = function ClientUI_ResourceStrings() {
}


Type.registerNamespace('ClientUI.ViewModels');

////////////////////////////////////////////////////////////////////////////////
// ClientUI.ViewModels.EntityLink

ClientUI.ViewModels.EntityLink = function ClientUI_ViewModels_EntityLink() {
}
ClientUI.ViewModels.EntityLink.prototype = {
    source: null,
    overflowedSource: null,
    target: null,
    overflowedTarget: null,
    id: null
}


////////////////////////////////////////////////////////////////////////////////
// ClientUI.ViewModels.EntityNodeLink

ClientUI.ViewModels.EntityNodeLink = function ClientUI_ViewModels_EntityNodeLink(node, link, weight) {
    this.target = node;
    this.weight = weight;
    this.link = link;
}
ClientUI.ViewModels.EntityNodeLink.prototype = {
    target: null,
    link: null,
    weight: 0
}


////////////////////////////////////////////////////////////////////////////////
// ClientUI.ViewModels.EntityNode

ClientUI.ViewModels.EntityNode = function ClientUI_ViewModels_EntityNode(name, size) {
    this.links = [];
    this.name = name;
    this.size = size;
}
ClientUI.ViewModels.EntityNode._getStringValue = function ClientUI_ViewModels_EntityNode$_getStringValue(field, record) {
    var stringValue = null;
    if (field == null) {
        return null;
    }
    if (Object.keyExists(record.formattedValues, field + 'name')) {
        return record.formattedValues[field + 'name'];
    }
    else {
        var value = record.getAttributeValue(field);
        if (value == null) {
            return stringValue;
        }
        var valueType = Type.getInstanceType(value);
        switch (valueType.get_name()) {
            case 'EntityReference':
                stringValue = (value).name;
                break;
            default:
                stringValue = value.toString();
                break;
        }
    }
    return stringValue;
}
ClientUI.ViewModels.EntityNode.prototype = {
    name: null,
    size: null,
    id: null,
    x: null,
    y: null,
    sourceData: null,
    overflowNode: null,
    replacedByOverflow: null,
    children: null,
    _Children: null,
    linkedToIds: null,
    loadedActivities: false,
    loadedConnections: false,
    activityCount: 0,
    isActivity: false,
    parentNode: null,
    fixed: false,
    root: false,
    
    GetQuickViewForm: function ClientUI_ViewModels_EntityNode$GetQuickViewForm(vm) {
        var record = this.sourceData;
        if (record == null) {
            return null;
        }
        var form = [];
        if (Object.keyExists(vm.config.quickViewForms, record.logicalName)) {
            var $enum1 = ss.IEnumerator.getEnumerator(Object.keys(vm.config.quickViewForms[record.logicalName]));
            while ($enum1.moveNext()) {
                var field = $enum1.current;
                var label = vm.config.quickViewForms[record.logicalName][field];
                var value = ClientUI.ViewModels.EntityNode._getStringValue(field, record);
                form.add(new ClientUI.ViewModels.FormCell(label, value));
            }
        }
        return form;
    }
}


////////////////////////////////////////////////////////////////////////////////
// ClientUI.ViewModels.FormCell

ClientUI.ViewModels.FormCell = function ClientUI_ViewModels_FormCell(label, value) {
    this.label = label;
    this.value = value;
}
ClientUI.ViewModels.FormCell.prototype = {
    label: null,
    value: null
}


////////////////////////////////////////////////////////////////////////////////
// ClientUI.ViewModels.NetworkViewModel

ClientUI.ViewModels.NetworkViewModel = function ClientUI_ViewModels_NetworkViewModel(id, logicalName, config) {
    this.DemoMode = ko.observable(true);
    this.Iterations = ko.observable(0);
    this.LoadIsPaused = ko.observable(false);
    this.nodes = [];
    this.links = [];
    this.queue = [];
    this.idIndex = {};
    this.idIndexQueried = {};
    this.linkIndex = {};
    this.idIndexActivityLoad = {};
    this.idIndexConnectionLoad = {};
    this.emailDomains = {};
    this.ConnectionRoles = ko.observableArray();
    this._userIdIndex$1 = {};
    this._connectionRoleIndex$1 = {};
    this.highlightedEntitiesToRemove = ko.observableArray();
    this.HighlightedLinks = ko.observableArray();
    this.highlightedLinksToRemove = ko.observableArray();
    this._userOrTeamIds$1 = {};
    this._pendingLinks$1 = {};
    this.SelectedNode = ko.observable(new ClientUI.ViewModels.EntityNode(null, 0));
    this.SelectedConnectionRoles = ko.observableArray();
    this.tooManyNodes = ko.observable(false);
    this.userContacts = {};
    ClientUI.ViewModels.NetworkViewModel.initializeBase(this);
    this.UsersAndTeams = ko.observableArray();
    this.highlightedEntities = ko.observableArray();
    this.SelectedUserId = ko.observable();
    this.rootEntityId = id;
    this.rootEntityId.value = this._normalisedGuid$1(this.rootEntityId.value);
    this.rootEntityLogicalName = logicalName;
    this.config = config;
    if (typeof(this.config) === 'undefined') {
        this.cancelRequested = true;
        window.setTimeout(ss.Delegate.create(this, function() {
            this._reportError$1(new Error(ClientUI.ResourceStrings.noConfigurationError));
        }), 100);
        return;
    }
    if ((typeof(this.config.demoModeInitialState) !== 'undefined')) {
        this.DemoMode(config.demoModeInitialState);
    }
    if ((typeof(this.config.iterationCountPerLoad) === 'undefined')) {
        this.config.iterationCountPerLoad = this._maxIterations$1;
    }
    if (this.config.entities != null && Object.getKeyCount(this.config.entities) > 0) {
        this.queue.enqueue(new ClientUI.ViewModels.QueuedLoad([[ id.value ]], this.config.entities[logicalName], null));
    }
    this._demoTick$1();
}
ClientUI.ViewModels.NetworkViewModel._getEmailDomain$1 = function ClientUI_ViewModels_NetworkViewModel$_getEmailDomain$1(emailAddress) {
    var domain = null;
    var emailParts = emailAddress.split('@');
    if (emailParts.length > 1) {
        domain = emailParts[1];
    }
    return domain;
}
ClientUI.ViewModels.NetworkViewModel._setLinkedIds$1 = function ClientUI_ViewModels_NetworkViewModel$_setLinkedIds$1(target, source, sourceEntity, targetEntity, link) {
    if (source.linkedToIds == null) {
        source.linkedToIds = {};
    }
    if (target.linkedToIds == null) {
        target.linkedToIds = {};
    }
    source.linkedToIds[targetEntity.id] = new ClientUI.ViewModels.EntityNodeLink(target, link, 1);
    target.linkedToIds[sourceEntity.id] = new ClientUI.ViewModels.EntityNodeLink(source, link, 1);
}
ClientUI.ViewModels.NetworkViewModel.prototype = {
    config: null,
    _demoCounter$1: 0,
    SelectedUserId: null,
    rootEntityId: null,
    rootEntityLogicalName: null,
    _currentLoad: null,
    suspendLayout: false,
    UsersAndTeams: null,
    highlightedEntities: null,
    _lastInterrupt$1: null,
    cancelRequested: false,
    _maxIterations$1: 10,
    _maxQueueItems$1: 1000,
    _queueIterations$1: 0,
    _startX$1: 0,
    _startY$1: 0,
    _overflowMax$1: 5,
    
    add_onNewNodes: function ClientUI_ViewModels_NetworkViewModel$add_onNewNodes(value) {
        this.__onNewNodes$1 = ss.Delegate.combine(this.__onNewNodes$1, value);
    },
    remove_onNewNodes: function ClientUI_ViewModels_NetworkViewModel$remove_onNewNodes(value) {
        this.__onNewNodes$1 = ss.Delegate.remove(this.__onNewNodes$1, value);
    },
    
    __onNewNodes$1: null,
    
    add_onSelectedNodesCleared: function ClientUI_ViewModels_NetworkViewModel$add_onSelectedNodesCleared(value) {
        this.__onSelectedNodesCleared$1 = ss.Delegate.combine(this.__onSelectedNodesCleared$1, value);
    },
    remove_onSelectedNodesCleared: function ClientUI_ViewModels_NetworkViewModel$remove_onSelectedNodesCleared(value) {
        this.__onSelectedNodesCleared$1 = ss.Delegate.remove(this.__onSelectedNodesCleared$1, value);
    },
    
    __onSelectedNodesCleared$1: null,
    
    add_onSelectedNodesAdded: function ClientUI_ViewModels_NetworkViewModel$add_onSelectedNodesAdded(value) {
        this.__onSelectedNodesAdded$1 = ss.Delegate.combine(this.__onSelectedNodesAdded$1, value);
    },
    remove_onSelectedNodesAdded: function ClientUI_ViewModels_NetworkViewModel$remove_onSelectedNodesAdded(value) {
        this.__onSelectedNodesAdded$1 = ss.Delegate.remove(this.__onSelectedNodesAdded$1, value);
    },
    
    __onSelectedNodesAdded$1: null,
    
    add_onSelectedLinksAdded: function ClientUI_ViewModels_NetworkViewModel$add_onSelectedLinksAdded(value) {
        this.__onSelectedLinksAdded$1 = ss.Delegate.combine(this.__onSelectedLinksAdded$1, value);
    },
    remove_onSelectedLinksAdded: function ClientUI_ViewModels_NetworkViewModel$remove_onSelectedLinksAdded(value) {
        this.__onSelectedLinksAdded$1 = ss.Delegate.remove(this.__onSelectedLinksAdded$1, value);
    },
    
    __onSelectedLinksAdded$1: null,
    
    add_onInfoBoxClose: function ClientUI_ViewModels_NetworkViewModel$add_onInfoBoxClose(value) {
        this.__onInfoBoxClose$1 = ss.Delegate.combine(this.__onInfoBoxClose$1, value);
    },
    remove_onInfoBoxClose: function ClientUI_ViewModels_NetworkViewModel$remove_onInfoBoxClose(value) {
        this.__onInfoBoxClose$1 = ss.Delegate.remove(this.__onInfoBoxClose$1, value);
    },
    
    __onInfoBoxClose$1: null,
    
    add_onZoom: function ClientUI_ViewModels_NetworkViewModel$add_onZoom(value) {
        this.__onZoom$1 = ss.Delegate.combine(this.__onZoom$1, value);
    },
    remove_onZoom: function ClientUI_ViewModels_NetworkViewModel$remove_onZoom(value) {
        this.__onZoom$1 = ss.Delegate.remove(this.__onZoom$1, value);
    },
    
    __onZoom$1: null,
    
    CancelCommand: function ClientUI_ViewModels_NetworkViewModel$CancelCommand() {
        this.cancelRequested = true;
        this.isBusy(false);
    },
    
    DrillIntoCommand: function ClientUI_ViewModels_NetworkViewModel$DrillIntoCommand() {
        var record = this.SelectedNode().sourceData;
        ClientUI.ViewModels.XrmForm.openRecordInNewWindow(record.id, record.logicalName);
    },
    
    LoadMoreCommand: function ClientUI_ViewModels_NetworkViewModel$LoadMoreCommand() {
        this.LoadIsPaused(false);
        this.Iterations(0);
        this.processQueue();
    },
    
    CloseInfoBoxCommand: function ClientUI_ViewModels_NetworkViewModel$CloseInfoBoxCommand() {
        this.__onInfoBoxClose$1(this, null);
    },
    
    ConnectionRoleClickCommand: function ClientUI_ViewModels_NetworkViewModel$ConnectionRoleClickCommand(that, role) {
        this.DemoMode(false);
        this.SelectedUserId(null);
        this._unselectAll$1();
        if (!Object.keyExists(this._connectionRoleIndex$1, role)) {
            return;
        }
        if (this.SelectedConnectionRoles().contains(role)) {
            this.SelectedConnectionRoles.remove(role);
            var $enum1 = ss.IEnumerator.getEnumerator(Object.keys(this._connectionRoleIndex$1[role]));
            while ($enum1.moveNext()) {
                var key = $enum1.current;
                this._selectLink$1(key, false);
            }
        }
        else {
            this.SelectedConnectionRoles.push(role);
            var $enum2 = ss.IEnumerator.getEnumerator(Object.keys(this._connectionRoleIndex$1[role]));
            while ($enum2.moveNext()) {
                var key = $enum2.current;
                this._selectLink$1(key, true);
            }
        }
        if (this.__onSelectedLinksAdded$1 != null) {
            this.__onSelectedLinksAdded$1(this, null);
        }
        if (this.__onSelectedNodesAdded$1 != null) {
            this.__onSelectedNodesAdded$1(this, null);
        }
    },
    
    _selectLink$1: function ClientUI_ViewModels_NetworkViewModel$_selectLink$1(key, select) {
        if (select) {
            this.HighlightedLinks.push(key);
        }
        else {
            this.HighlightedLinks.remove(key);
            this.highlightedLinksToRemove.push(key);
        }
        var link = this.linkIndex[key];
        if (link != null) {
            var source = link.source;
            if (source != null) {
                var sourceRef = (source.sourceData).toEntityReference();
                if (select) {
                    this.highlightedEntities.push(sourceRef);
                }
                else {
                    this.highlightedEntities.remove(sourceRef);
                    this.highlightedEntitiesToRemove.push(sourceRef);
                }
            }
            var target = link.target;
            if (target != null) {
                var targetRef = (target.sourceData).toEntityReference();
                if (select) {
                    this.highlightedEntities.push(targetRef);
                }
                else {
                    this.highlightedEntities.remove(targetRef);
                    this.highlightedEntitiesToRemove.push(targetRef);
                }
            }
        }
    },
    
    UserClickCommand: function ClientUI_ViewModels_NetworkViewModel$UserClickCommand(that, user) {
        this.DemoMode(false);
        this._unselectAll$1();
        var selected = false;
        if (user == null || that.SelectedUserId() === user.id) {
            that.SelectedUserId(null);
        }
        else {
            that.SelectedUserId(user.id);
            selected = true;
        }
        if (selected) {
            var $enum1 = ss.IEnumerator.getEnumerator(Object.keys(user.Parties));
            while ($enum1.moveNext()) {
                var partyid = $enum1.current;
                var party = user.Parties[partyid];
                var key = this._getIdIndexKeyEntityRef$1(party);
                if (Object.keyExists(this.idIndex, key) && this.idIndex[key].replacedByOverflow != null) {
                    var overflow = this.idIndex[this._getIdIndexKeyEntityRef$1(party)].replacedByOverflow.sourceData;
                    this.highlightedEntities.push(new SparkleXrm.Sdk.EntityReference(new SparkleXrm.Sdk.Guid(overflow.id), overflow.logicalName, null));
                }
                else {
                    this.highlightedEntities.push(party);
                }
            }
        }
        if (this.__onSelectedNodesAdded$1 != null) {
            this.__onSelectedNodesAdded$1(this, null);
        }
        if (this.__onSelectedLinksAdded$1 != null) {
            this.__onSelectedLinksAdded$1(this, null);
        }
    },
    
    _unselectAll$1: function ClientUI_ViewModels_NetworkViewModel$_unselectAll$1() {
        this.SelectedConnectionRoles.removeAll();
        var $enum1 = ss.IEnumerator.getEnumerator(this.HighlightedLinks());
        while ($enum1.moveNext()) {
            var link = $enum1.current;
            this.highlightedLinksToRemove.push(link);
        }
        this.HighlightedLinks.removeAll();
        var $enum2 = ss.IEnumerator.getEnumerator(this.highlightedEntities());
        while ($enum2.moveNext()) {
            var key = $enum2.current;
            this.highlightedEntitiesToRemove.push(key);
        }
        this.highlightedEntities.removeAll();
    },
    
    ZoomInCommand: function ClientUI_ViewModels_NetworkViewModel$ZoomInCommand() {
        if (this.__onZoom$1 != null) {
            this.__onZoom$1(this, 1);
        }
    },
    
    ZoomOutCommand: function ClientUI_ViewModels_NetworkViewModel$ZoomOutCommand() {
        if (this.__onZoom$1 != null) {
            this.__onZoom$1(this, -1);
        }
    },
    
    DemoModeClickCommand: function ClientUI_ViewModels_NetworkViewModel$DemoModeClickCommand() {
        var mode = !this.DemoMode();
        this.DemoMode(mode);
        if (mode) {
            this._demoCounter$1 = 0;
            this._demoTick$1();
        }
    },
    
    _isCancelRequested$1: function ClientUI_ViewModels_NetworkViewModel$_isCancelRequested$1() {
        if (this.cancelRequested) {
            this.isBusy(false);
        }
        return this.cancelRequested;
    },
    
    _reportError$1: function ClientUI_ViewModels_NetworkViewModel$_reportError$1(ex) {
        this.isBusy(true);
        this.isBusyMessage(ex.message);
    },
    
    processQueue: function ClientUI_ViewModels_NetworkViewModel$processQueue() {
        if (this._isCancelRequested$1()) {
            return;
        }
        if (!this.isBusy()) {
            this.isBusy(true);
        }
        this._queueIterations$1++;
        this._trace$1('--------------------{0}', [ this._queueIterations$1 ]);
        var queueString = '';
        var $enum1 = ss.IEnumerator.getEnumerator(this.queue);
        while ($enum1.moveNext()) {
            var load = $enum1.current;
            queueString += load.entity.logicalName;
            if (load.join != null) {
                if (load.join.rightEntity === 'connection' || load.join.rightEntity === 'activity') {
                    queueString += ('[' + load.join.rightEntity + ']');
                }
                else {
                    queueString += ('[' + load.join.leftEntity + '.' + load.join.leftAttribute + ' = ' + load.join.rightEntity + '.' + load.join.rightAttribute + ']');
                }
            }
            queueString += ' | ';
        }
        this._trace$1('Queue = {0}', [ queueString ]);
        this._trace$1('--------------------{0}', [ this._queueIterations$1 ]);
        if (this._queueIterations$1 > this._maxQueueItems$1) {
            this._pauseLoadWithMessage$1(ClientUI.ResourceStrings.possibleInfiniteLoop);
            this._queueIterations$1 = 0;
            return;
        }
        if (this._currentLoad == null) {
            this._currentLoad = this.queue.dequeue();
            if (this._currentLoad == null) {
                this._addPendingQueuedLoads$1();
                this.raiseOnNodesChanged();
                this._currentLoad = this.queue.dequeue();
                if (this._currentLoad == null) {
                    this.isBusy(false);
                    this._trace$1('End of Queue', null);
                    return;
                }
            }
        }
        this._tryToShrinkQueue$1();
        if (this._currentLoad.join == null) {
            if (!this._nextIteration$1()) {
                return;
            }
            this._trace$1('Entity Query {0} ', [ this._currentLoad.entity.logicalName ]);
            this._setMessage$1('Loading', this._currentLoad.entity.displayName, 0, 0);
            var correctedIds = [];
            var $enum2 = ss.IEnumerator.getEnumerator(this._currentLoad.ids);
            while ($enum2.moveNext()) {
                var id = $enum2.current;
                var key = this._getIdIndexString$1(this._currentLoad.entity.logicalName, id);
                if (Object.keyExists(this.idIndexQueried, key)) {
                    this._removePendingID$1(this._currentLoad.entity.logicalName, id);
                    continue;
                }
                else {
                    this.idIndexQueried[key] = key;
                }
                correctedIds.add(id);
            }
            if (correctedIds.length > 0) {
                var op = (this._currentLoad.entity.hierarchical) ? 'eq-above-under' : 'eq';
                var idValues = this._getValues$1(this._currentLoad.entity.logicalName, this._currentLoad.entity.idAttribute, op, correctedIds);
                var rootQuery = String.format('\n\n<!-- ' + this.isBusyMessage() + '-->\n\n' + this._currentLoad.entity.fetchXml, idValues);
                this._loadFetchXml$1(this._currentLoad, rootQuery);
            }
            else {
                this._currentLoad = null;
                this._callNextProcessQueue$1();
                return;
            }
        }
        else if (this._currentLoad.join.rightEntity === 'activity') {
            this._currentLoad = null;
            this._getActivities$1();
            return;
        }
        else if (this._currentLoad.join.rightEntity === 'connection') {
            this._currentLoad = null;
            this._getConnections$1();
            return;
        }
        else if (this._currentLoad.join != null) {
            this._trace$1('Join Query {0}->{1}', [ this._currentLoad.join.leftEntity, this._currentLoad.join.rightEntity ]);
            this._setMessage$1('Joining', this._currentLoad.entity.displayName, 0, 0);
            if (this._currentLoad.ids.length > 0) {
                var joinIds = [];
                var $enum3 = ss.IEnumerator.getEnumerator(this._currentLoad.ids);
                while ($enum3.moveNext()) {
                    var id = $enum3.current;
                    var record = this.idIndex[this._getIdIndexKeyEntityRef$1(new SparkleXrm.Sdk.EntityReference(new SparkleXrm.Sdk.Guid(id), this._currentLoad.join.leftEntity, null))];
                    if (record != null) {
                        var idValue = (record.sourceData).getAttributeValue(this._currentLoad.join.leftAttribute);
                        if (idValue != null) {
                            if (Type.getInstanceType(idValue) === SparkleXrm.Sdk.Guid) {
                                joinIds.add((idValue).value);
                            }
                            else if (Type.getInstanceType(idValue) === SparkleXrm.Sdk.EntityReference) {
                                joinIds.add((idValue).id.value);
                            }
                        }
                    }
                }
                if (joinIds.length > 0) {
                    this._trace$1('Found {0} Join IDs', [ joinIds.length ]);
                    var join = this._currentLoad.join;
                    var op = (this._currentLoad.entity.hierarchical && join.rightAttribute === this._currentLoad.entity.idAttribute) ? 'eq-above-under' : 'eq';
                    var joinIdsCorrected = [];
                    if (join.excludeIds == null) {
                        join.excludeIds = {};
                    }
                    var $enum4 = ss.IEnumerator.getEnumerator(joinIds);
                    while ($enum4.moveNext()) {
                        var id = $enum4.current;
                        if (!Object.keyExists(join.excludeIds, 'ID' + id)) {
                            joinIdsCorrected.add(id);
                            join.excludeIds['ID' + id] = id;
                        }
                    }
                    if (joinIdsCorrected.length > 0) {
                        this._trace$1('Correted Join IDS {0}', [ joinIdsCorrected.length ]);
                        var joinXml = String.format('\n\n<!-- ' + this.isBusyMessage() + '-->\n\n' + this._currentLoad.entity.fetchXml, this._getValues$1(this._currentLoad.entity.logicalName, this._currentLoad.join.rightAttribute, op, joinIdsCorrected));
                        this._loadFetchXml$1(this._currentLoad, joinXml);
                    }
                    else {
                        this._trace$1('Join supressed due to infinite loop possibility', null);
                        this._currentLoad = null;
                        this._callNextProcessQueue$1();
                    }
                    return;
                }
                else {
                    this._trace$1('Nothing to Load', null);
                    this._currentLoad = null;
                    this._callNextProcessQueue$1();
                    return;
                }
            }
            else {
                this._callNextProcessQueue$1();
                return;
            }
        }
    },
    
    _tryToShrinkQueue$1: function ClientUI_ViewModels_NetworkViewModel$_tryToShrinkQueue$1() {
        var nextLoad = this.queue.peek();
        if (nextLoad != null && (nextLoad.entity.logicalName === this._currentLoad.entity.logicalName)) {
            var thisJoin = (this._currentLoad.join != null) ? this._currentLoad.join.rightEntity : null;
            var nextJoin = (nextLoad.join != null) ? nextLoad.join.rightEntity : null;
            if (thisJoin === nextJoin) {
                this._trace$1('Next Load is same join', null);
                var load = this.queue.dequeue();
                this._currentLoad.ids.addRange(load.ids);
            }
        }
    },
    
    _addPendingQueuedLoads$1: function ClientUI_ViewModels_NetworkViewModel$_addPendingQueuedLoads$1() {
        var $enum1 = ss.IEnumerator.getEnumerator(Object.keys(this._pendingLinks$1));
        while ($enum1.moveNext()) {
            var pendingEntity = $enum1.current;
            this._trace$1('Pending Entity {0}', [ pendingEntity ]);
            var entity = this.config.entities[pendingEntity];
            if (entity != null && Object.getKeyCount(this._pendingLinks$1[pendingEntity]) > 0) {
                this.queue.enqueue(new ClientUI.ViewModels.QueuedLoad(Object.keys(this._pendingLinks$1[pendingEntity]), entity, null));
                this._trace$1('Queued {0}', [ pendingEntity ]);
            }
            else {
                this._trace$1('Nothing to Load', null);
            }
        }
    },
    
    _callNextProcessQueue$1: function ClientUI_ViewModels_NetworkViewModel$_callNextProcessQueue$1() {
        if (this._lastInterrupt$1 == null) {
            this._lastInterrupt$1 = Date.get_now();
        }
        if ((this._lastInterrupt$1 - Date.get_now()) > 3000) {
            this._lastInterrupt$1 = Date.get_now();
            window.setTimeout(ss.Delegate.create(this, function() {
                this.processQueue();
            }), 100);
        }
        else {
            this.processQueue();
        }
    },
    
    _nextIteration$1: function ClientUI_ViewModels_NetworkViewModel$_nextIteration$1() {
        var i = this.Iterations();
        i++;
        this.Iterations(i);
        if (i < this.config.iterationCountPerLoad) {
            return true;
        }
        else {
            this._pauseLoadWithMessage$1(ClientUI.ResourceStrings.recordLimitExceeded);
            return false;
        }
    },
    
    _pauseLoadWithMessage$1: function ClientUI_ViewModels_NetworkViewModel$_pauseLoadWithMessage$1(message) {
        this.isBusyMessage(message);
        this.LoadIsPaused(true);
        this.raiseOnNodesChanged();
    },
    
    _trace$1: function ClientUI_ViewModels_NetworkViewModel$_trace$1(message, values) {
        if (this.config.trace) {
            var value1 = (values != null && values.length > 0) ? values[0] : null;
            var value2 = (values != null && values.length > 1) ? values[1] : null;
            var value3 = (values != null && values.length > 2) ? values[2] : null;
            var value4 = (values != null && values.length > 3) ? values[3] : null;
            var value5 = (values != null && values.length > 4) ? values[4] : null;
            var trace = String.format(message, value1, value2, value3, value4);
            ss.Debug.writeln(trace);
        }
    },
    
    _setMessage$1: function ClientUI_ViewModels_NetworkViewModel$_setMessage$1(status, entity, i, total) {
        var message = String.format(ClientUI.ResourceStrings.statusMessage, status, entity, this.nodes.length);
        this.isBusyMessage(message);
        this.isBusyProgress((i / total) * 100);
    },
    
    _getParentEntity$1: function ClientUI_ViewModels_NetworkViewModel$_getParentEntity$1(record, parentAttributeName, parentEnityLogicalName) {
        var parent = record.getAttributeValue(parentAttributeName);
        var parentid = '';
        if (Type.getInstanceType(parent) === SparkleXrm.Sdk.Guid) {
            parentid = (parent).value;
        }
        else if (Type.getInstanceType(parent) === SparkleXrm.Sdk.EntityReference) {
            parentid = (parent).id.value;
            parentEnityLogicalName = (parent).logicalName;
        }
        var parentNode = null;
        if (parent != null) {
            var key = parentEnityLogicalName + parentid;
            if (Object.keyExists(this.idIndex, key)) {
                parentNode = this.idIndex[key];
            }
        }
        return parentNode;
    },
    
    _loadFetchXml$1: function ClientUI_ViewModels_NetworkViewModel$_loadFetchXml$1(load, rootQuery) {
        this._setMessage$1(ClientUI.ResourceStrings.querying, load.entity.displayName, 0, 0);
        SparkleXrm.Sdk.OrganizationServiceProxy.beginRetrieveMultiple(rootQuery, ss.Delegate.create(this, function(state) {
            try {
                var rootResults = SparkleXrm.Sdk.OrganizationServiceProxy.endRetrieveMultiple(state, SparkleXrm.Sdk.Entity);
                this._trace$1('Query for {0} returned {1} records', [ load.entity.logicalName, rootResults.get_entities().get_count() ]);
                this._processQueryResults$1(load, rootResults);
            }
            catch (ex) {
                this._reportError$1(ex);
                return;
            }
        }));
    },
    
    _processQueryResults$1: function ClientUI_ViewModels_NetworkViewModel$_processQueryResults$1(load, rootResults) {
        var total = rootResults.get_entities().get_count();
        var index = 0;
        var ids = [];
        var $enum1 = ss.IEnumerator.getEnumerator(rootResults.get_entities());
        while ($enum1.moveNext()) {
            var record = $enum1.current;
            if (this._isCancelRequested$1()) {
                return;
            }
            this._setMessage$1(ClientUI.ResourceStrings.processing, load.entity.displayName, index, total);
            this._removePendingID$1(record.logicalName, record.id);
            index++;
            ids.add(record.id);
            var name = record.getAttributeValueString((load.entity.nameAttribute == null) ? 'name' : load.entity.nameAttribute);
            var rootNode = this.rootEntityId.value === record.id.toLowerCase();
            if (rootNode) {
                window.document.title = name;
            }
            if (!this._isAlsoAUser$1(record) && !this._indexContainsEntity$1(record)) {
                var newNode = new ClientUI.ViewModels.EntityNode(name, 100);
                newNode.root = rootNode;
                newNode.x = this._startX$1;
                newNode.y = this._startY$1;
                newNode.sourceData = record;
                if (!this._addEntity$1(record, newNode, load.entity, false)) {
                    return;
                }
                var owner = record.getAttributeValueEntityReference('ownerid');
                if (owner != null) {
                    var user = this.getUserOrTeamReference(owner);
                    user.Parties[record.id] = record.toEntityReference();
                }
            }
        }
        this._trace$1('Linking to Parents {0} {1} records', [ load.entity.logicalName, rootResults.get_entities().get_count() ]);
        index = 0;
        var $enum2 = ss.IEnumerator.getEnumerator(rootResults.get_entities());
        while ($enum2.moveNext()) {
            var record = $enum2.current;
            if (this._isCancelRequested$1()) {
                return;
            }
            this._setMessage$1(ClientUI.ResourceStrings.linking, load.entity.displayName, index, total);
            var thisNode = this._getEntity$1(record);
            if (thisNode == null) {
                continue;
            }
            var parentNode = this._getParentEntity$1(record, load.entity.parentAttributeId, load.entity.logicalName);
            if (parentNode != null) {
                this._addLink$1(thisNode, parentNode, false);
            }
            if (load.join != null) {
                this._trace$1('Adding backlinks {0}->{1}', [ load.entity.logicalName, load.join.rightEntity ]);
                var joinedNode = this._getParentEntity$1(record, load.join.rightAttribute, load.join.rightEntity);
                if (joinedNode != null) {
                    this._addLink$1(thisNode, joinedNode, false);
                }
                else {
                }
            }
        }
        if (ids.length > 0) {
            if (load.entity.joins != null) {
                this._trace$1('Adding Joins to {0}', [ ids.length ]);
                var $enum3 = ss.IEnumerator.getEnumerator(load.entity.joins);
                while ($enum3.moveNext()) {
                    var join = $enum3.current;
                    var joinedTo = this.config.entities[join.rightEntity];
                    if (joinedTo != null) {
                        this._trace$1('Queing Join  {0}.{1} -> {2}.{3} {4}', [ join.leftEntity, join.leftAttribute, join.rightEntity, join.rightAttribute, join.name ]);
                        this.queue.enqueue(new ClientUI.ViewModels.QueuedLoad(ids, joinedTo, join));
                    }
                }
            }
            if (load.entity.loadActivities) {
                this.queue.enqueue(new ClientUI.ViewModels.QueuedLoad(ids, load.entity, this._newJoinSetting$1(load.entity.logicalName, 'activity')));
            }
            if (load.entity.loadConnections) {
                this.queue.enqueue(new ClientUI.ViewModels.QueuedLoad(ids, load.entity, this._newJoinSetting$1(load.entity.logicalName, 'connection')));
            }
        }
        this._currentLoad = null;
        this._callNextProcessQueue$1();
    },
    
    _normalisedGuid$1: function ClientUI_ViewModels_NetworkViewModel$_normalisedGuid$1(guid) {
        if (guid.substr(0, 1) === '{') {
            guid = guid.substr(1, 36);
        }
        return guid.toLowerCase();
    },
    
    _getValues$1: function ClientUI_ViewModels_NetworkViewModel$_getValues$1(logicalName, attribute, op, ids) {
        var values = "<filter type='or'>";
        if (op === 'eq-above-under') {
            var $enum1 = ss.IEnumerator.getEnumerator(ids);
            while ($enum1.moveNext()) {
                var id = $enum1.current;
                values += String.format("<condition attribute='{0}' operator='{2}' value='{1}'/>", attribute, id, 'eq-or-above');
                values += String.format("<condition attribute='{0}' operator='{2}' value='{1}'/>", attribute, id, 'under');
            }
        }
        else {
            var $enum2 = ss.IEnumerator.getEnumerator(ids);
            while ($enum2.moveNext()) {
                var id = $enum2.current;
                values += String.format("<condition attribute='{0}' operator='{2}' value='{1}'/>", attribute, id, op);
            }
        }
        values += '</filter>';
        return values;
    },
    
    _getActivities$1: function ClientUI_ViewModels_NetworkViewModel$_getActivities$1() {
        if (this._isCancelRequested$1()) {
            return;
        }
        this._setMessage$1(ClientUI.ResourceStrings.loading, ClientUI.ResourceStrings.activities, 0, 0);
        var parties = '';
        var $enum1 = ss.IEnumerator.getEnumerator(Object.keys(this.idIndexActivityLoad));
        while ($enum1.moveNext()) {
            var key = $enum1.current;
            parties += '<value>' + (this.idIndexActivityLoad[key].sourceData).id + '</value>';
        }
        Object.clearKeys(this.idIndexActivityLoad);
        if (!!parties) {
            var fetchXml = String.format(this.config.acitvityFetchXml, parties);
            SparkleXrm.Sdk.OrganizationServiceProxy.beginRetrieveMultiple(fetchXml, ss.Delegate.create(this, function(state2) {
                if (this._isCancelRequested$1()) {
                    return;
                }
                var rootResults2;
                var total = 0;
                try {
                    rootResults2 = SparkleXrm.Sdk.OrganizationServiceProxy.endRetrieveMultiple(state2, SparkleXrm.Sdk.Entity);
                    total = rootResults2.get_entities().get_count();
                }
                catch (ex) {
                    this._reportError$1(ex);
                    return;
                }
                SparkleXrm.DelegateItterator.callbackItterate(ss.Delegate.create(this, function(index, nextCallBack, errorCallBack) {
                    if (this._isCancelRequested$1()) {
                        return;
                    }
                    this._processActivity$1(rootResults2, index, total);
                    nextCallBack();
                }), total, ss.Delegate.create(this, function() {
                    this._loadUsers$1(ss.Delegate.create(this, function() {
                        this._currentLoad = null;
                        this._callNextProcessQueue$1();
                        return;
                    }));
                }), function(ex) {
                });
            }));
        }
        else {
            this._currentLoad = null;
            this._callNextProcessQueue$1();
            return;
        }
    },
    
    _processActivity$1: function ClientUI_ViewModels_NetworkViewModel$_processActivity$1(rootResults2, index, total) {
        var record = rootResults2.get_entities().get_item(index);
        this._setMessage$1(ClientUI.ResourceStrings.processing, ClientUI.ResourceStrings.activities, index, total);
        var newNode;
        record.logicalName = record.getAttributeValueString('activitytypecode');
        var alreadyAdded = false;
        if (!this._indexContainsEntity$1(record)) {
            newNode = new ClientUI.ViewModels.EntityNode(record.getAttributeValueString('name'), 100);
            newNode.isActivity = true;
            newNode.x = this._startX$1;
            newNode.y = this._startY$1;
            newNode.sourceData = record;
            if (!this._addEntity$1(record, newNode, null, true)) {
                return;
            }
        }
        else {
            newNode = this._getEntity$1(record);
            alreadyAdded = true;
        }
        var allParties = record.getAttributeValue('allparties');
        var i = 0;
        var overflow = false;
        var $enum1 = ss.IEnumerator.getEnumerator(allParties.get_entities());
        while ($enum1.moveNext()) {
            var item = $enum1.current;
            if (this._isCancelRequested$1()) {
                return;
            }
            if (!(index % 20)) {
                this._setMessage$1(ClientUI.ResourceStrings.processing, ClientUI.ResourceStrings.activityLinks, index, total);
            }
            i++;
            var party = item;
            var partyid = party.partyid;
            if (partyid != null) {
                var linkToExisting = this._addLinkIfLoaded$1(newNode, partyid, true);
                if (linkToExisting == null) {
                    if (party.partyid.logicalName === 'systemuser' || party.partyid.logicalName === 'team') {
                        party.activityid.logicalName = (newNode.sourceData).logicalName;
                        var user = this.getUserOrTeamReference(party.partyid);
                        user.Parties[party.activityid.id.toString()] = party.activityid;
                    }
                    else {
                        this._addPendingLink$1(newNode, partyid);
                    }
                }
                else {
                    if (newNode.parentNode == null) {
                        newNode.parentNode = linkToExisting.target;
                        newNode.parentNode.activityCount++;
                    }
                    var overflowAlreadyAdded = false;
                    var overflowed = newNode.parentNode.activityCount > this._overflowMax$1 && newNode.parentNode === linkToExisting.target;
                    if (alreadyAdded) {
                        overflow = false;
                    }
                    if (overflowed) {
                        if (newNode.parentNode.overflowNode == null) {
                            var overflowNode = new ClientUI.ViewModels.EntityNode(ClientUI.ResourceStrings.doubleClickToExpand, 1);
                            overflowNode.id = 'overflow' + partyid.id.value;
                            overflowNode.parentNode = newNode.parentNode;
                            overflowNode.x = this._startX$1;
                            overflowNode.y = this._startY$1;
                            var overflowEntity = new SparkleXrm.Sdk.Entity('overflow');
                            overflowEntity.id = 'overflow' + (newNode.sourceData).id;
                            overflowNode.sourceData = overflowEntity;
                            this.nodes.add(overflowNode);
                            newNode.parentNode.overflowNode = overflowNode;
                            newNode.replacedByOverflow = overflowNode;
                        }
                        else if (linkToExisting.source.parentNode === linkToExisting.target && !alreadyAdded) {
                            overflowAlreadyAdded = true;
                            linkToExisting.source.replacedByOverflow = linkToExisting.target.overflowNode;
                        }
                        if (newNode.replacedByOverflow != null) {
                            if (newNode.replacedByOverflow.children == null) {
                                newNode.replacedByOverflow.children = [];
                            }
                            if (!newNode.replacedByOverflow.children.contains(newNode)) {
                                newNode.replacedByOverflow.children.add(newNode);
                            }
                        }
                    }
                    if (!overflowAlreadyAdded) {
                        if (linkToExisting.source.replacedByOverflow != null) {
                            linkToExisting.source = linkToExisting.source.replacedByOverflow;
                            linkToExisting.source.links.add(linkToExisting);
                            if (!linkToExisting.id.startsWith('overflow')) {
                                linkToExisting.id = 'overflow' + linkToExisting.id;
                            }
                        }
                        if (!Object.keyExists(this.linkIndex, linkToExisting.id)) {
                            this.linkIndex[linkToExisting.id] = linkToExisting;
                            this.links.add(linkToExisting);
                        }
                        else {
                        }
                    }
                }
            }
        }
        if (newNode.replacedByOverflow == null) {
            if (!alreadyAdded) {
                this.nodes.add(newNode);
            }
        }
    },
    
    _getConnections$1: function ClientUI_ViewModels_NetworkViewModel$_getConnections$1() {
        if (this._isCancelRequested$1()) {
            return;
        }
        this._setMessage$1(ClientUI.ResourceStrings.loading, ClientUI.ResourceStrings.connections, 0, 0);
        var values = '';
        var names = '';
        var $enum1 = ss.IEnumerator.getEnumerator(Object.keys(this.idIndexConnectionLoad));
        while ($enum1.moveNext()) {
            var key = $enum1.current;
            values += '<value>' + (this.idIndexConnectionLoad[key].sourceData).id + '</value>';
            names += this.idIndexConnectionLoad[key].name + ' , ';
        }
        Object.clearKeys(this.idIndexConnectionLoad);
        if (!!values) {
            var fetchXml = String.format(this.config.connectionFetchXml, values);
            SparkleXrm.Sdk.OrganizationServiceProxy.beginRetrieveMultiple(fetchXml, ss.Delegate.create(this, function(state2) {
                if (this._isCancelRequested$1()) {
                    return;
                }
                var rootResults2 = null;
                var total = 0;
                try {
                    rootResults2 = SparkleXrm.Sdk.OrganizationServiceProxy.endRetrieveMultiple(state2, ClientUI.ViewModels.Connection);
                    total = rootResults2.get_entities().get_count();
                }
                catch (ex) {
                    this._reportError$1(ex);
                    return;
                }
                SparkleXrm.DelegateItterator.callbackItterate(ss.Delegate.create(this, function(index, nextCallBack, errorCallBack) {
                    if (this._isCancelRequested$1()) {
                        return;
                    }
                    this._processConnection$1(rootResults2, index, total);
                    nextCallBack();
                }), total, ss.Delegate.create(this, function() {
                    this._currentLoad = null;
                    this._callNextProcessQueue$1();
                    return;
                }), function(ex) {
                });
            }));
        }
        else {
            this._currentLoad = null;
            this._callNextProcessQueue$1();
            return;
        }
    },
    
    _processConnection$1: function ClientUI_ViewModels_NetworkViewModel$_processConnection$1(rootResults2, index, total) {
        var record = rootResults2.get_entities().get_item(index);
        this._setMessage$1(ClientUI.ResourceStrings.processing, ClientUI.ResourceStrings.connections, index, total);
        var record1id = record.record1id;
        var record2id = record.record2id;
        var role1 = record.record1roleid;
        var role2 = record.record2roleid;
        var linkFrom = this._getEntityFromReference$1(record1id);
        var linkToExisting = this._addLinkIfLoaded$1(linkFrom, record2id, false);
        var record1UserOrTeam = record1id.logicalName === 'systemuser' || record1id.logicalName === 'team';
        var record2UserOrTeam = record2id.logicalName === 'systemuser' || record2id.logicalName === 'team';
        if (!record1UserOrTeam && !record2UserOrTeam && linkToExisting != null) {
            if (role1 != null) {
                if (!Object.keyExists(this._connectionRoleIndex$1, role1.name)) {
                    this._connectionRoleIndex$1[role1.name] = {};
                }
                if (!Object.keyExists(this._connectionRoleIndex$1[role1.name], linkToExisting.id)) {
                    this._connectionRoleIndex$1[role1.name][linkToExisting.id] = linkToExisting;
                }
                if (!this.ConnectionRoles().contains(role1.name)) {
                    this.ConnectionRoles.push(role1.name);
                }
            }
            if (role2 != null) {
                if (!Object.keyExists(this._connectionRoleIndex$1, role2.name)) {
                    this._connectionRoleIndex$1[role2.name] = {};
                }
                if (!Object.keyExists(this._connectionRoleIndex$1[role2.name], linkToExisting.id)) {
                    this._connectionRoleIndex$1[role2.name][linkToExisting.id] = linkToExisting;
                }
                if (!this.ConnectionRoles().contains(role2.name)) {
                    this.ConnectionRoles.push(role2.name);
                }
            }
        }
        if (linkToExisting == null) {
            if (record2UserOrTeam) {
                var user = this.getUserOrTeamReference(record2id);
                user.Parties[record2id.id.toString()] = record1id;
            }
            else if (record1UserOrTeam) {
                var user = this.getUserOrTeamReference(record1id);
                user.Parties[record1id.id.toString()] = record2id;
            }
            else {
                this._addPendingLink$1(linkFrom, record2id);
            }
        }
    },
    
    _isAlsoAUserFromEntityReference$1: function ClientUI_ViewModels_NetworkViewModel$_isAlsoAUserFromEntityReference$1(record) {
        var entityRecord = new SparkleXrm.Sdk.Entity(record.logicalName);
        entityRecord.id = record.id.value;
        return this._isAlsoAUser$1(entityRecord);
    },
    
    _isAlsoAUser$1: function ClientUI_ViewModels_NetworkViewModel$_isAlsoAUser$1(record) {
        if (Object.keyExists(this.userContacts, record.id)) {
            return true;
        }
        else {
            if ((record.logicalName === 'contact' && record.getAttributeValue('systemuserid') != null)) {
                this.userContacts[record.id] = record;
                return true;
            }
            if (record.logicalName === 'contact') {
                var emailAddress = record.getAttributeValueString('emailaddress1');
                if (emailAddress != null) {
                    var domain = ClientUI.ViewModels.NetworkViewModel._getEmailDomain$1(emailAddress);
                    return Object.keyExists(this.emailDomains, domain);
                }
            }
        }
        return false;
    },
    
    getUserOrTeamReference: function ClientUI_ViewModels_NetworkViewModel$getUserOrTeamReference(userOrTeamId) {
        var user;
        if (!Object.keyExists(this._userOrTeamIds$1, userOrTeamId.id.toString())) {
            user = new ClientUI.ViewModels.UserOrTeam();
            user.id = userOrTeamId.id.toString();
            user.isTeam = (userOrTeamId.logicalName === 'team');
            user.logicalName = (user.isTeam) ? 'team' : 'systemuser';
            user.fullname = null;
            user.Parties = {};
            this._userOrTeamIds$1[user.id] = user;
        }
        else {
            user = this._userOrTeamIds$1[userOrTeamId.id.toString()];
        }
        return user;
    },
    
    _loadUsers$1: function ClientUI_ViewModels_NetworkViewModel$_loadUsers$1(callback) {
        var useridValues = '';
        var teamidValues = '';
        var $enum1 = ss.IEnumerator.getEnumerator(Object.keys(this._userOrTeamIds$1));
        while ($enum1.moveNext()) {
            var userid = $enum1.current;
            var userOrTeam = this._userOrTeamIds$1[userid];
            if (userOrTeam.fullname == null) {
                if (userOrTeam.isTeam) {
                    teamidValues += '<value>' + userOrTeam.id + '</value>';
                }
                else {
                    useridValues += '<value>' + userOrTeam.id + '</value>';
                }
            }
        }
        var count = 1;
        var complete = 0;
        if (!useridValues.length && !teamidValues.length) {
            callback();
            return;
        }
        count = (useridValues.length > 0 && teamidValues.length > 0) ? 2 : 1;
        var userQuery = String.format("<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false' nolock='true'>\r\n                              <entity name='systemuser'>\r\n                                <attribute name='fullname' />\r\n                                <attribute name='systemuserid' />\r\n                                <attribute name='internalemailaddress'/>\r\n                                <filter type='and'>\r\n                                  <condition attribute='systemuserid' operator='in' >{0}</condition>\r\n                                </filter>\r\n                              </entity>\r\n                            </fetch>", useridValues);
        var teamQuery = String.format("<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false' nolock='true'>\r\n                              <entity name='team'>\r\n                                <attribute name='name' />\r\n                                <attribute name='teamid' />\r\n                                <filter type='and'>\r\n                                  <condition attribute='teamid' operator='in' >{0}</condition>\r\n                                </filter>\r\n                              </entity>\r\n                            </fetch>", teamidValues);
        if (useridValues.length > 0) {
            SparkleXrm.Sdk.OrganizationServiceProxy.beginRetrieveMultiple(userQuery, ss.Delegate.create(this, function(state) {
                if (this._isCancelRequested$1()) {
                    return;
                }
                var users = SparkleXrm.Sdk.OrganizationServiceProxy.endRetrieveMultiple(state, ClientUI.ViewModels.UserOrTeam);
                var $enum1 = ss.IEnumerator.getEnumerator(users.get_entities());
                while ($enum1.moveNext()) {
                    var user = $enum1.current;
                    if (this._isCancelRequested$1()) {
                        return;
                    }
                    this._userOrTeamIds$1[user.id].fullname = user.fullname;
                    user.Parties = this._userOrTeamIds$1[user.id].Parties;
                    if (user.internalemailaddress != null) {
                        var domain = ClientUI.ViewModels.NetworkViewModel._getEmailDomain$1(user.internalemailaddress);
                        if (!Object.keyExists(this.emailDomains, domain)) {
                            this.emailDomains[domain] = domain;
                        }
                    }
                    this.UsersAndTeams.push(user);
                }
                complete++;
                if (complete >= count) {
                    callback();
                }
            }));
        }
        if (teamidValues.length > 0) {
            SparkleXrm.Sdk.OrganizationServiceProxy.beginRetrieveMultiple(teamQuery, ss.Delegate.create(this, function(state) {
                if (this._isCancelRequested$1()) {
                    return;
                }
                var teams = SparkleXrm.Sdk.OrganizationServiceProxy.endRetrieveMultiple(state, ClientUI.ViewModels.UserOrTeam);
                var $enum1 = ss.IEnumerator.getEnumerator(teams.get_entities());
                while ($enum1.moveNext()) {
                    var team = $enum1.current;
                    if (this._isCancelRequested$1()) {
                        return;
                    }
                    team.fullname = team.name;
                    this._userOrTeamIds$1[team.id].fullname = team.name;
                    team.isTeam = true;
                    team.Parties = this._userOrTeamIds$1[team.id].Parties;
                    this.UsersAndTeams.push(team);
                }
                complete++;
                if (complete >= count) {
                    callback();
                }
            }));
        }
    },
    
    _newJoinSetting$1: function ClientUI_ViewModels_NetworkViewModel$_newJoinSetting$1(fromEntity, joinEntity) {
        var join = {};
        join.leftEntity = fromEntity;
        join.rightEntity = joinEntity;
        return join;
    },
    
    _getLinkId$1: function ClientUI_ViewModels_NetworkViewModel$_getLinkId$1(fromId, toId) {
        var key;
        if (fromId.compareTo(toId) > 0) {
            key = fromId.replaceAll('-', '') + '_' + toId.replaceAll('-', '');
        }
        else {
            key = toId.replaceAll('-', '') + '_' + fromId.replaceAll('-', '');
        }
        return 'L' + key;
    },
    
    _removePendingID$1: function ClientUI_ViewModels_NetworkViewModel$_removePendingID$1(logicalName, id) {
        if (Object.keyExists(this._pendingLinks$1, logicalName)) {
            var linkIds = this._pendingLinks$1[logicalName];
            if (Object.keyExists(linkIds, id)) {
                var link = linkIds[id];
                delete linkIds[id];
            }
        }
    },
    
    _addLink$1: function ClientUI_ViewModels_NetworkViewModel$_addLink$1(target, source, delayAdd) {
        var sourceEntity = source.sourceData;
        var targetEntity = target.sourceData;
        if ((target.linkedToIds != null) && (source.linkedToIds != null)) {
            if (Object.keyExists(target.linkedToIds, sourceEntity.id) && Object.keyExists(source.linkedToIds, targetEntity.id)) {
                return target.linkedToIds[sourceEntity.id].link;
            }
        }
        var link = new ClientUI.ViewModels.EntityLink();
        link.source = source;
        link.target = target;
        ClientUI.ViewModels.NetworkViewModel._setLinkedIds$1(link.target, link.source, sourceEntity, targetEntity, link);
        if (!source.isActivity && link.source.parentNode != null && link.source.parentNode.overflowNode != null) {
            link.source = link.source.parentNode.overflowNode;
            link.source.links.add(link);
        }
        else if (source.isActivity && link.source.overflowNode != null) {
            link.source = link.source.overflowNode;
            link.source.links.add(link);
        }
        if (!target.isActivity && link.target.parentNode != null && link.target.parentNode.overflowNode != null) {
            link.target = link.target.parentNode.overflowNode;
        }
        else if (target.isActivity && link.target.overflowNode != null) {
            link.target = link.target.overflowNode;
            link.target.links.add(link);
        }
        link.id = this._getLinkId$1(sourceEntity.id, targetEntity.id);
        if (!Object.keyExists(this.linkIndex, link.id)) {
            if (!delayAdd) {
                this.linkIndex[link.id] = link;
                this.links.add(link);
            }
        }
        return link;
    },
    
    _addPendingLink$1: function ClientUI_ViewModels_NetworkViewModel$_addPendingLink$1(newNode, pendingEntity) {
        this._trace$1('Add Pending Link {0} {1} -> {2} {3}', [ (newNode.sourceData).logicalName, newNode.name, pendingEntity.logicalName, pendingEntity.id ]);
        var pending = new ClientUI.ViewModels.PendingLink();
        pending.source = newNode;
        pending.target = pendingEntity;
        if (!Object.keyExists(this._pendingLinks$1, pendingEntity.logicalName)) {
            this._pendingLinks$1[pendingEntity.logicalName] = {};
        }
        if (!this._isAlsoAUserFromEntityReference$1(pendingEntity)) {
            this._pendingLinks$1[pendingEntity.logicalName][pendingEntity.id.toString()] = pending;
        }
        return pending;
    },
    
    _addLinkIfLoaded$1: function ClientUI_ViewModels_NetworkViewModel$_addLinkIfLoaded$1(target, source, delayAdd) {
        var sourceNode = this._getEntityFromReference$1(source);
        if (sourceNode != null) {
            var link = this._addLink$1(sourceNode, target, delayAdd);
            return link;
        }
        return null;
    },
    
    _addEntity$1: function ClientUI_ViewModels_NetworkViewModel$_addEntity$1(record, newNode, settings, delayAdd) {
        if (!((this.nodes.length + 1) % 40)) {
            this.__onZoom$1(this, -1);
        }
        var key = this._getIdIndexKey$1(record);
        this.idIndex[key] = newNode;
        if (settings != null) {
            if (!newNode.loadedActivities && settings.loadActivities) {
                this.idIndexActivityLoad[key] = newNode;
                newNode.loadedActivities = true;
            }
            if (!newNode.loadedConnections && settings.loadConnections) {
                this.idIndexConnectionLoad[key] = newNode;
                newNode.loadedConnections = true;
            }
        }
        if (!delayAdd) {
            this.nodes.add(newNode);
        }
        return true;
    },
    
    _getIdIndexKey$1: function ClientUI_ViewModels_NetworkViewModel$_getIdIndexKey$1(record) {
        return record.logicalName + record.id;
    },
    
    _getIdIndexKeyEntityRef$1: function ClientUI_ViewModels_NetworkViewModel$_getIdIndexKeyEntityRef$1(record) {
        return record.logicalName + record.id.toString();
    },
    
    _getIdIndexString$1: function ClientUI_ViewModels_NetworkViewModel$_getIdIndexString$1(logicalName, id) {
        return logicalName + id;
    },
    
    _indexContainsEntity$1: function ClientUI_ViewModels_NetworkViewModel$_indexContainsEntity$1(record) {
        return Object.keyExists(this.idIndex, this._getIdIndexKey$1(record));
    },
    
    _indexContainsEntityReference$1: function ClientUI_ViewModels_NetworkViewModel$_indexContainsEntityReference$1(record) {
        return Object.keyExists(this.idIndex, this._getIdIndexKeyEntityRef$1(record));
    },
    
    _getEntity$1: function ClientUI_ViewModels_NetworkViewModel$_getEntity$1(record) {
        return this.idIndex[this._getIdIndexKey$1(record)];
    },
    
    _getEntityFromReference$1: function ClientUI_ViewModels_NetworkViewModel$_getEntityFromReference$1(record) {
        return this.idIndex[this._getIdIndexKeyEntityRef$1(record)];
    },
    
    raiseOnNodesChanged: function ClientUI_ViewModels_NetworkViewModel$raiseOnNodesChanged() {
        if (!this.cancelRequested) {
            if (this.__onNewNodes$1 != null) {
                this.__onNewNodes$1(this, null);
            }
        }
    },
    
    raiseOnNodesChangedWithNoFastForward: function ClientUI_ViewModels_NetworkViewModel$raiseOnNodesChangedWithNoFastForward() {
        if (!this.cancelRequested) {
            if (this.__onNewNodes$1 != null) {
                this.__onNewNodes$1(this, new ss.EventArgs());
            }
        }
    },
    
    expandOverflow: function ClientUI_ViewModels_NetworkViewModel$expandOverflow(entityNode) {
        this.SelectedUserId(null);
        this.SelectedConnectionRoles.removeAll();
        this._unselectAll$1();
        this.__onSelectedNodesAdded$1(this, null);
        this.__onSelectedLinksAdded$1(this, null);
        if (entityNode.children != null) {
            var expandedCount = 0;
            var $enum1 = ss.IEnumerator.getEnumerator(entityNode.children);
            while ($enum1.moveNext()) {
                var node = $enum1.current;
                node.x = entityNode.x;
                node.y = entityNode.y;
                node.fixed = false;
                if (!this.nodes.contains(node)) {
                    this.nodes.add(node);
                }
                node.replacedByOverflow = null;
                expandedCount++;
                var $dict2 = node.linkedToIds;
                for (var $key3 in $dict2) {
                    var linkedTo = { key: $key3, value: $dict2[$key3] };
                    var link = new ClientUI.ViewModels.EntityLink();
                    link.source = node;
                    link.target = linkedTo.value.target;
                    link.id = this._getLinkId$1((node.sourceData).id, (link.target.sourceData).id);
                    if (Object.keyExists(this.linkIndex, link.id)) {
                        link = this.linkIndex[link.id];
                    }
                    else {
                        this.linkIndex[link.id] = link;
                        this.links.add(link);
                    }
                }
                if (expandedCount === this._overflowMax$1 && entityNode.children.length > (this._overflowMax$1 + 1)) {
                    break;
                }
            }
            entityNode.children.removeRange(0, expandedCount);
            entityNode.parentNode.activityCount = entityNode.children.length;
            if (!entityNode.children.length) {
                entityNode.children = null;
                this.nodes.remove(entityNode);
                var $enum4 = ss.IEnumerator.getEnumerator(entityNode.links);
                while ($enum4.moveNext()) {
                    var link = $enum4.current;
                    delete this.linkIndex[link.id];
                    this.links.remove(link);
                }
            }
            this.raiseOnNodesChangedWithNoFastForward();
        }
    },
    
    _demoTick$1: function ClientUI_ViewModels_NetworkViewModel$_demoTick$1() {
        if (!this.DemoMode()) {
            return;
        }
        var userCount = this.UsersAndTeams().length;
        var connectionCount = this.ConnectionRoles().length;
        var connectionIndex = (this._demoCounter$1 - userCount);
        if (connectionIndex >= this.ConnectionRoles().length) {
            this._demoCounter$1 = 0;
            this.UserClickCommand(this, null);
        }
        else if (this._demoCounter$1 < userCount) {
            var users = this.UsersAndTeams();
            var userId = users[this._demoCounter$1].id;
            this.UserClickCommand(this, this._userOrTeamIds$1[userId]);
            this._demoCounter$1++;
        }
        else if (connectionIndex >= 0 && connectionIndex < connectionCount) {
            var roles = this.ConnectionRoles();
            var role = roles[connectionIndex];
            this.ConnectionRoleClickCommand(this, role);
            this._demoCounter$1++;
        }
        this.DemoMode(true);
        if (this.DemoMode()) {
            window.setTimeout(ss.Delegate.create(this, this._demoTick$1), (this.config.demoTickLength != null && this.config.demoTickLength > 500) ? this.config.demoTickLength : 2000);
        }
    }
}


////////////////////////////////////////////////////////////////////////////////
// ClientUI.ViewModels.PendingLink

ClientUI.ViewModels.PendingLink = function ClientUI_ViewModels_PendingLink() {
}
ClientUI.ViewModels.PendingLink.prototype = {
    target: null,
    source: null
}


////////////////////////////////////////////////////////////////////////////////
// ClientUI.ViewModels.UserOrTeam

ClientUI.ViewModels.UserOrTeam = function ClientUI_ViewModels_UserOrTeam() {
    ClientUI.ViewModels.UserOrTeam.initializeBase(this, [ 'systemuser' ]);
    this.Parties = {};
}
ClientUI.ViewModels.UserOrTeam.prototype = {
    fullname: null,
    name: null,
    internalemailaddress: null,
    isTeam: false,
    Parties: null
}


////////////////////////////////////////////////////////////////////////////////
// ClientUI.ViewModels.ActivityParty

ClientUI.ViewModels.ActivityParty = function ClientUI_ViewModels_ActivityParty() {
    ClientUI.ViewModels.ActivityParty.initializeBase(this, [ 'activityparty' ]);
}
ClientUI.ViewModels.ActivityParty.prototype = {
    activityid: null,
    partyid: null
}


////////////////////////////////////////////////////////////////////////////////
// ClientUI.ViewModels.Connection

ClientUI.ViewModels.Connection = function ClientUI_ViewModels_Connection() {
    ClientUI.ViewModels.Connection.initializeBase(this, [ 'connection' ]);
}
ClientUI.ViewModels.Connection.prototype = {
    connectionid: null,
    record1id: null,
    record2id: null,
    record1roleid: null,
    record2roleid: null
}


////////////////////////////////////////////////////////////////////////////////
// ClientUI.ViewModels.QueuedLoad

ClientUI.ViewModels.QueuedLoad = function ClientUI_ViewModels_QueuedLoad(ids, entity, join) {
    this.ids = ids;
    this.entity = entity;
    this.join = join;
}
ClientUI.ViewModels.QueuedLoad.prototype = {
    ids: null,
    entity: null,
    join: null
}


////////////////////////////////////////////////////////////////////////////////
// ClientUI.ViewModels.XrmForm

ClientUI.ViewModels.XrmForm = function ClientUI_ViewModels_XrmForm() {
}
ClientUI.ViewModels.XrmForm.openRecordInNewWindow = function ClientUI_ViewModels_XrmForm$openRecordInNewWindow(id, typeName) {
    try {
        var url = '{0}/userdefined/edit.aspx?etc={3}&id=%7b{2}%7d';
        var etc = Mscrm.EntityPropUtil.EntityTypeName2CodeMap[typeName];
        var height = 1300;
        var width = 900;
        var serverUrl = String.format(url, Xrm.Page.context.getClientUrl(), typeName, id.replaceAll('{', '').replaceAll('}', ''), etc);
        var name = id.replaceAll('{', '').replaceAll('}', '').replaceAll('-', '');
        var win = window.open(serverUrl, 'drillOpen', String.format('height={0},width={1}', height, width));
        win.focus();
    }
    catch ($e1) {
    }
}


Type.registerNamespace('ClientUI.Views');

////////////////////////////////////////////////////////////////////////////////
// ClientUI.Views.NetworkView

ClientUI.Views.NetworkView = function ClientUI_Views_NetworkView(viewModel) {
    this._infoBoxClosed = Date.get_now();
    this.height = $(window).height();
    this.width = $(window).width();
    this.vm = viewModel;
    this._force = d3.layout.force().size([ this.width, this.height ]).linkDistance(150).friction(0.7).charge(-700).on('tick', ss.Delegate.create(this, this.tick));
    this._zoom = d3.behavior.zoom().scaleExtent([ this._minZoom, this._maxZoom ]).center([ this.width / 2, this.height / 2 ]).on('zoom', ss.Delegate.create(this, this.zoomed));
    this._svg = d3.select('#networksvg').attr('width', this.width).attr('height', this.height).call(this._zoom).append('g');
    this._dragBehavior = this._force.drag().on('dragstart', ss.Delegate.create(this, this.dragstart));
    this._svGexactTip = d3.select('#infoBox').style('opacity', 0);
    this._nodeData = this._force.nodes();
    this._linkData = this._force.links();
    this._link = this._svg.selectAll('.link');
    this._node = this._svg.selectAll('.node');
    this.update();
    SparkleXrm.ViewBase.registerViewModel(this.vm);
    this.vm.add_onNewNodes(ss.Delegate.create(this, this._onNodesChange));
    this.vm.add_onSelectedNodesAdded(ss.Delegate.create(this, this._vm_OnSelectedNodesAdded));
    this.vm.add_onSelectedNodesCleared(ss.Delegate.create(this, this._vm_OnSelectedNodesCleared));
    this.vm.add_onSelectedLinksAdded(ss.Delegate.create(this, this._vm_OnSelectedLinksAdded));
    this.vm.add_onInfoBoxClose(ss.Delegate.create(this, this._vm_OnInfoBoxClose));
    this.vm.add_onZoom(ss.Delegate.create(this, this._vm_OnZoom));
    $(window).resize(ss.Delegate.create(this, this._onResize));
    $(ss.Delegate.create(this, function() {
        window.setTimeout(ss.Delegate.create(this, function() {
            this.vm.processQueue();
        }), 10);
    }));
}
ClientUI.Views.NetworkView.Init = function ClientUI_Views_NetworkView$Init() {
    SparkleXrm.LocalisedContentLoader.fallBackLCID = 0;
    SparkleXrm.LocalisedContentLoader.supportedLCIDs.add(1033);
    SparkleXrm.LocalisedContentLoader.loadContent('dev1_/js/NetworkViewConfig.js', 1033, function() {
        var data = ClientUI.Views.NetworkView._getQueryStringData();
        if (!Object.keyExists(data, 'etn')) {
            data['etn'] = 'account';
        }
        var vm = new ClientUI.ViewModels.NetworkViewModel(new SparkleXrm.Sdk.Guid(data['id']), data['etn'], window.GraphOptions);
        ClientUI.Views.NetworkView.view = new ClientUI.Views.NetworkView(vm);
    });
}
ClientUI.Views.NetworkView._getQueryStringData = function ClientUI_Views_NetworkView$_getQueryStringData() {
    if (window.location.search.length > 0) {
        var dataParameter = window.location.search.split('=');
        var data = decodeURIComponent(dataParameter[1]);
        var parameters = data.split('&');
        var dataPairs = {};
        var $enum1 = ss.IEnumerator.getEnumerator(parameters);
        while ($enum1.moveNext()) {
            var param = $enum1.current;
            var nameValue = param.split('=');
            dataPairs[nameValue[0]] = nameValue[1];
        }
        return dataPairs;
    }
    return null;
}
ClientUI.Views.NetworkView._highlightLink = function ClientUI_Views_NetworkView$_highlightLink(key) {
    var link = d3.select('#' + key);
    if (link != null) {
        link.attr('filter', 'url(#selected-glow)');
        link.attr('class', 'link link-selected');
    }
}
ClientUI.Views.NetworkView._unHighlightLink = function ClientUI_Views_NetworkView$_unHighlightLink(key) {
    var link = d3.select('#' + key);
    if (link != null) {
        link.attr('filter', null);
        link.attr('class', 'link');
    }
}
ClientUI.Views.NetworkView._getFilter = function ClientUI_Views_NetworkView$_getFilter(d) {
    if (d.root) {
        return 'url(#root-node-glow)';
    }
    else {
        return '';
    }
}
ClientUI.Views.NetworkView._getSize = function ClientUI_Views_NetworkView$_getSize(node) {
    var entity = (node.sourceData);
    var size = 0;
    switch (entity.logicalName) {
        case 'account':
            size = 15;
            break;
        case 'contact':
            size = 10;
            break;
        default:
            size = 20;
            break;
    }
    size = size + ((node.root) ? 2 : 0);
    return size;
}
ClientUI.Views.NetworkView.prototype = {
    vm: null,
    width: 960,
    height: 500,
    _minZoom: 0.2,
    _maxZoom: 1.5,
    _root: null,
    _force: null,
    _svg: null,
    _link: null,
    _node: null,
    _dragBehavior: null,
    _svGexactTip: null,
    _zoom: null,
    _nodeData: null,
    _linkData: null,
    _toggle: false,
    _stickyInfoBox: true,
    _currentInfoBoxNode: null,
    _infoBoxPinned: false,
    
    _vm_OnSelectedLinksAdded: function ClientUI_Views_NetworkView$_vm_OnSelectedLinksAdded(sender, e) {
        var $enum1 = ss.IEnumerator.getEnumerator(this.vm.highlightedLinksToRemove());
        while ($enum1.moveNext()) {
            var key = $enum1.current;
            ClientUI.Views.NetworkView._unHighlightLink(key);
        }
        this.vm.highlightedLinksToRemove.removeAll();
        var $enum2 = ss.IEnumerator.getEnumerator(this.vm.HighlightedLinks());
        while ($enum2.moveNext()) {
            var key = $enum2.current;
            ClientUI.Views.NetworkView._highlightLink(key);
        }
    },
    
    _vm_OnZoom: function ClientUI_Views_NetworkView$_vm_OnZoom(sender, direction) {
        this._zoomControl(direction);
    },
    
    _vm_OnInfoBoxClose: function ClientUI_Views_NetworkView$_vm_OnInfoBoxClose(sender, e) {
        this._hideInfoBox();
    },
    
    _vm_OnSelectedNodesCleared: function ClientUI_Views_NetworkView$_vm_OnSelectedNodesCleared(sender, e) {
        var $enum1 = ss.IEnumerator.getEnumerator(this.vm.highlightedEntities());
        while ($enum1.moveNext()) {
            var entity = $enum1.current;
            this._unSelectEntity(entity);
        }
    },
    
    _vm_OnSelectedNodesAdded: function ClientUI_Views_NetworkView$_vm_OnSelectedNodesAdded(sender, e) {
        var $enum1 = ss.IEnumerator.getEnumerator(this.vm.highlightedEntitiesToRemove());
        while ($enum1.moveNext()) {
            var entity = $enum1.current;
            this._unSelectEntity(entity);
        }
        this.vm.highlightedEntitiesToRemove.removeAll();
        var $enum2 = ss.IEnumerator.getEnumerator(this.vm.highlightedEntities());
        while ($enum2.moveNext()) {
            var entity = $enum2.current;
            this._selectEntity(entity);
        }
    },
    
    _onNodesChange: function ClientUI_Views_NetworkView$_onNodesChange(sender, e) {
        if (this.vm.suspendLayout) {
            return;
        }
        this._nodeData.clear();
        var nodeList = this.vm.nodes;
        for (var i = 0; i < nodeList.length; i++) {
            this._nodeData.push(nodeList[i]);
        }
        this._linkData.clear();
        var linkList = this.vm.links;
        for (var i = 0; i < linkList.length; i++) {
            this._linkData.push(linkList[i]);
        }
        this.update();
        if (e == null) {
            this.fastForward(this._force, 0.01, 100);
        }
        else {
            this._force.friction(0.01);
            this.fastForward(this._force, 0.09, 100);
            this._force.friction(0.9);
            this.update();
        }
    },
    
    _onResize: function ClientUI_Views_NetworkView$_onResize(e) {
        var sheight = $(window).height();
        var swidth = $(window).width();
        d3.select('#networksvg').attr('width', swidth).attr('height', sheight);
    },
    
    zoomed: function ClientUI_Views_NetworkView$zoomed(d, i) {
        this._svg.attr('transform', 'translate(' + d3.event.translate + ')scale(' + d3.event.scale + ')');
    },
    
    dragstart: function ClientUI_Views_NetworkView$dragstart(d, i) {
        d.fixed = true;
        d3.event.sourceEvent.stopPropagation();
    },
    
    dragmove: function ClientUI_Views_NetworkView$dragmove(d, i) {
        d.px += d3.event.dx;
        d.py += d3.event.dy;
        d.x += d3.event.dx;
        d.y += d3.event.dy;
        this.tick(null, 0);
    },
    
    dragend: function ClientUI_Views_NetworkView$dragend(d, i) {
        d.fixed = true;
        this.tick(null, 0);
        this._force.resume();
    },
    
    tick: function ClientUI_Views_NetworkView$tick(n, i) {
        this._link.attr('x1', function(d) {
            return d.source.x;
        }).attr('y1', function(d) {
            return d.source.y;
        }).attr('x2', function(d) {
            return d.target.x;
        }).attr('y2', function(d) {
            return d.target.y;
        });
        this._node.attr('cx', function(d) {
            return d.x;
        }).attr('cy', function(d) {
            return d.y;
        });
        this._node.attr('transform', function(d) {
            return 'translate(' + d.x + ',' + d.y + ')';
        });
    },
    
    _getID: function ClientUI_Views_NetworkView$_getID(id) {
        return 'ID' + id.replaceAll('-', '').toLowerCase();
    },
    
    _selectEntity: function ClientUI_Views_NetworkView$_selectEntity(entity) {
        var key = this._getID(entity.id.toString());
        d3.select('#' + key).selectAll('.entityImage').attr('filter', 'url(#selected-glow)');
        d3.select('#' + key).selectAll('.entityImage').transition().attr('transform', 'scale(0.6)').transition().attr('transform', 'scale(2)');
    },
    
    _unSelectEntity: function ClientUI_Views_NetworkView$_unSelectEntity(entity) {
        var key = this._getID(entity.id.toString());
        var node = this.vm.idIndex[key];
        d3.select('#' + key).selectAll('.entityImage').attr('filter', 'url(#no-glow)');
        d3.select('#' + key).selectAll('.entityImage').transition().attr('transform', 'scale(1)');
    },
    
    update: function ClientUI_Views_NetworkView$update() {
        this._svg.selectAll('text').remove();
        this._svg.selectAll('image').remove();
        this._link = this._link.data(this._linkData, function(d) {
            var id = (d).id;
            return id;
        });
        this._node = this._node.data(this._nodeData, function(d) {
            var id = ((d).sourceData).id;
            return id;
        });
        this._link.enter().insert('svg:line', '.node').attr('id', function(d) {
            return d.id;
        }).attr('class', 'link');
        this._node.enter().append('svg:g').attr('id', ss.Delegate.create(this, function(d) {
            var entity = d.sourceData;
            return this._getID(entity.id);
        })).attr('class', 'node').attr('filter', 'url(#blur1)').on('click', ss.Delegate.create(this, function(d, i) {
            this._highlightNode(d);
            this._showInfoBox(d, true);
        })).on('mouseover', ss.Delegate.create(this, function(d, i) {
            this._highlightNode(d);
            if (!this._infoBoxPinned) {
                this._showInfoBox(d, false);
            }
        })).on('mouseout', ss.Delegate.create(this, function(d, i) {
            this._unHighlightNode(d);
            if (this._stickyInfoBox || this._infoBoxPinned) {
                return;
            }
            this._hideInfoBox();
        })).on('dblclick', ss.Delegate.create(this, function(d, i) {
            d.fixed = false;
            d3.event.stopPropagation();
            var entityNode = d;
            this.vm.expandOverflow(entityNode);
        })).call(this._dragBehavior);
        this._node.append('svg:image').attr('class', 'chromeImage').attr('xlink:href', function(d) {
            var entity = (d.sourceData);
            switch (entity.logicalName) {
                case 'account':
                case 'contact':
                    return '../images/network.png';
                default:
                    return null;
            }
        }).attr('x', ss.Delegate.create(this, function(d) {
            return this.getXY(d, 1.5);
        })).attr('y', ss.Delegate.create(this, function(d) {
            return this.getXY(d, 1.5);
        })).attr('width', ss.Delegate.create(this, function(d) {
            return this.getHeightWidth(d, 1.5);
        })).attr('height', ss.Delegate.create(this, function(d) {
            return this.getHeightWidth(d, 1.5);
        })).attr('visibility', function(d) {
            var entity = (d.sourceData);
            switch (entity.logicalName) {
                case 'account':
                case 'contact':
                    return null;
                default:
                    return 'hidden';
            }
        }).attr('filter', function(d) {
            return ClientUI.Views.NetworkView._getFilter(d);
        });
        this._node.append('svg:image').attr('class', 'entityImage').attr('xlink:href', function(d) {
            var entity = (d.sourceData);
            switch (entity.logicalName) {
                case 'overflow':
                    return '../images/overflow.png';
                case 'account':
                    return '../images/account.png';
                case 'contact':
                    return '../images/contact.png';
                case 'incident':
                    return '/_imgs/Navbar/ActionImgs/Cases_32.png';
                case 'contract':
                    return '/_imgs/Navbar/ActionImgs/Contract_32.png';
                case 'opportunity':
                    return '/_imgs/Navbar/ActionImgs/Opportunity_32.png';
                case 'lead':
                    return '/_imgs/Navbar/ActionImgs/Lead_32.png';
                case 'phonecall':
                    return '/_imgs/Navbar/ActionImgs/PhoneCall_32.png';
                case 'email':
                    return '/_imgs/Navbar/ActionImgs/Email_32.png';
                case 'task':
                    return '/_imgs/Navbar/ActionImgs/Task_32.png';
                case 'appointment':
                    return '/_imgs/Navbar/ActionImgs/Appointment_32.png';
                default:
                    return '/_imgs/Navbar/ActionImgs/Documents_32.png';
            }
        }).attr('x', ss.Delegate.create(this, function(d) {
            return this.getXY(d, 0.5);
        })).attr('y', ss.Delegate.create(this, function(d) {
            return this.getXY(d, 0.5);
        })).attr('width', ss.Delegate.create(this, function(d) {
            return this.getHeightWidth(d, 0.5);
        })).attr('height', ss.Delegate.create(this, function(d) {
            return this.getHeightWidth(d, 0.5);
        })).attr('filter', 'url(#blur2)');
        this._node.append('svg:text').attr('class', 'nodetext').attr('dx', function(d) {
            var entity = (d.sourceData);
            switch (entity.logicalName) {
                case 'overflow':
                    return -3;
                default:
                    return -15;
            }
        }).attr('dy', function(d) {
            var entity = (d.sourceData);
            switch (entity.logicalName) {
                case 'overflow':
                    return 3;
                default:
                    return -15;
            }
        }).text(ss.Delegate.create(this, function(d) {
            var entity = d.sourceData;
            if (entity.logicalName === 'overflow') {
                if (d.children != null) {
                    return d.children.length.toString();
                }
                return '';
            }
            else {
                var entitySetting = this.vm.config.entities[entity.logicalName];
                var name = entity.getAttributeValueString((entitySetting != null && entitySetting.nameAttribute != null) ? entitySetting.nameAttribute : 'name');
                if (name != null && name.length > 50) {
                    name = name.substr(0, 50) + '...';
                }
                return name;
            }
        }));
        this._link.exit().remove();
        this._node.exit().remove();
        this._force.start();
        var uniqueKeyCache = {};
        var $enum1 = ss.IEnumerator.getEnumerator(this.vm.links);
        while ($enum1.moveNext()) {
            var l = $enum1.current;
            var id = l.id;
            if (Object.keyExists(uniqueKeyCache, id)) {
                ss.Debug.writeln('Duplicate key ' + id);
            }
            else {
                uniqueKeyCache[id] = id;
            }
        }
        var $enum2 = ss.IEnumerator.getEnumerator(this.vm.nodes);
        while ($enum2.moveNext()) {
            var l = $enum2.current;
            var id = ((l).sourceData).id;
            if (Object.keyExists(uniqueKeyCache, id)) {
                ss.Debug.writeln('Duplicate key ' + id);
            }
            else {
                uniqueKeyCache[id] = id;
            }
        }
    },
    
    _hideInfoBox: function ClientUI_Views_NetworkView$_hideInfoBox() {
        this._infoBoxPinned = false;
        this._svGexactTip.style('opacity', '0');
        this._svGexactTip.transition().style('left', -1000 + 'px').style('top', -1000 + 'px');
    },
    
    _highlightNode: function ClientUI_Views_NetworkView$_highlightNode(d) {
        var thisnode1 = d3.select(d3.event.target.parentNode).selectAll('.chromeImage');
        var thisnode2 = d3.select(d3.event.target.parentNode).selectAll('.entityImage');
        thisnode1.transition().attr('transform', 'scale(1.2)');
        thisnode2.transition().attr('transform', 'scale(2)');
    },
    
    _unHighlightNode: function ClientUI_Views_NetworkView$_unHighlightNode(d) {
        var thisnode = d3.select(d3.event.target.parentNode).selectAll('image');
        thisnode.transition().attr('transform', 'scale(1)');
    },
    
    _showInfoBox: function ClientUI_Views_NetworkView$_showInfoBox(d, clicked) {
        var entityNode = d;
        this.vm.SelectedNode(entityNode);
        if (clicked) {
            if ((entityNode.sourceData).id === this._currentInfoBoxNode) {
                this._infoBoxPinned = false;
            }
            else {
                this._infoBoxPinned = true;
                this._currentInfoBoxNode = (entityNode.sourceData).id;
            }
        }
        this._svGexactTip.style('opacity', '1');
        var thisnode = d3.select(d3.event.target.parentNode).selectAll('image');
        var matrix = thisnode.node().getScreenCTM().translate(thisnode.node().getAttribute('cx'), thisnode.node().getAttribute('cy'));
        var swidth = $(window).width();
        var sheight = $(window).height();
        var left = (window.pageXOffset + matrix.e) + 50;
        var top = (window.pageYOffset + matrix.f) - 10;
        if (top + 100 > sheight) {
            top = sheight - 100;
        }
        left = 20;
        this._svGexactTip.transition().style('left', left + 'px').style('top', top + 'px');
    },
    
    getXY: function ClientUI_Views_NetworkView$getXY(node, multipler) {
        var size = 0;
        var entity = (node.sourceData);
        size = ClientUI.Views.NetworkView._getSize(node);
        return '-' + (size * multipler).toString() + 'px';
    },
    
    getHeightWidth: function ClientUI_Views_NetworkView$getHeightWidth(node, multipler) {
        var size = 0;
        var entity = (node.sourceData);
        size = ClientUI.Views.NetworkView._getSize(node) * 2;
        return (size * multipler).toString() + 'px';
    },
    
    fastForward: function ClientUI_Views_NetworkView$fastForward(layout, alpha, max) {
        var i = 0;
        while ((layout.alpha() > alpha) && (i < max) && !this.vm.cancelRequested) {
            layout.tick();
            i++;
        }
    },
    
    _zoomControl: function ClientUI_Views_NetworkView$_zoomControl(direction) {
        this._svg.call(this._zoom.event);
        var center0 = this._zoom.center();
        var translate0 = this._zoom.translate();
        var coordinates0 = this._coordinates(center0);
        var scale = this._zoom.scale();
        scale = scale * Math.pow(1.5, direction);
        if (scale <= this._minZoom) {
            scale = this._minZoom;
        }
        else if (scale >= this._maxZoom) {
            scale = this._maxZoom;
        }
        this._zoom.scale(scale);
        var center1 = this._point(coordinates0);
        this._zoom.translate([ translate0[0] + center0[0] - center1[0], translate0[1] + center0[1] - center1[1] ]);
        this._svg.transition().duration(750).call(this._zoom.event);
    },
    
    _coordinates: function ClientUI_Views_NetworkView$_coordinates(point) {
        var scale = this._zoom.scale();
        var translate = this._zoom.translate();
        return [ (point[0] - translate[0]) / scale, (point[1] - translate[1]) / scale ];
    },
    
    _point: function ClientUI_Views_NetworkView$_point(coordinates) {
        var scale = this._zoom.scale();
        var translate = this._zoom.translate();
        return [ coordinates[0] * scale + translate[0], coordinates[1] * scale + translate[1] ];
    }
}


ClientUI.ResourceStrings.registerClass('ClientUI.ResourceStrings');
ClientUI.ViewModels.EntityLink.registerClass('ClientUI.ViewModels.EntityLink');
ClientUI.ViewModels.EntityNodeLink.registerClass('ClientUI.ViewModels.EntityNodeLink');
ClientUI.ViewModels.EntityNode.registerClass('ClientUI.ViewModels.EntityNode');
ClientUI.ViewModels.FormCell.registerClass('ClientUI.ViewModels.FormCell');
ClientUI.ViewModels.NetworkViewModel.registerClass('ClientUI.ViewModels.NetworkViewModel', SparkleXrm.ViewModelBase);
ClientUI.ViewModels.PendingLink.registerClass('ClientUI.ViewModels.PendingLink');
ClientUI.ViewModels.UserOrTeam.registerClass('ClientUI.ViewModels.UserOrTeam', SparkleXrm.Sdk.Entity);
ClientUI.ViewModels.ActivityParty.registerClass('ClientUI.ViewModels.ActivityParty', SparkleXrm.Sdk.Entity);
ClientUI.ViewModels.Connection.registerClass('ClientUI.ViewModels.Connection', SparkleXrm.Sdk.Entity);
ClientUI.ViewModels.QueuedLoad.registerClass('ClientUI.ViewModels.QueuedLoad');
ClientUI.ViewModels.XrmForm.registerClass('ClientUI.ViewModels.XrmForm');
ClientUI.Views.NetworkView.registerClass('ClientUI.Views.NetworkView');
ClientUI.ResourceStrings.possibleInfiniteLoop = "Whoa! I'm going around in circles. Do you have an infinite loop in your configuration?";
ClientUI.ResourceStrings.loading = 'Loading';
ClientUI.ResourceStrings.connections = 'Connections';
ClientUI.ResourceStrings.processing = 'Processing';
ClientUI.ResourceStrings.querying = 'Querying';
ClientUI.ResourceStrings.linking = 'Linking';
ClientUI.ResourceStrings.recordLimitExceeded = "You've got lots to look at here!";
ClientUI.ResourceStrings.activities = 'Activities';
ClientUI.ResourceStrings.activityLinks = 'Activity Links';
ClientUI.ResourceStrings.doubleClickToExpand = 'Double Click to Expand';
ClientUI.ResourceStrings.statusMessage = 'Records {2}: {0} {1}...';
ClientUI.ResourceStrings.noConfigurationError = 'No Configuration';
ClientUI.ViewModels.UserOrTeam.entityLogicalName = 'systemuser';
ClientUI.ViewModels.ActivityParty.entityLogicalName = 'activityparty';
ClientUI.ViewModels.Connection.entityLogicalName = 'connection';
ClientUI.Views.NetworkView.view = null;
})(window.xrmjQuery);


