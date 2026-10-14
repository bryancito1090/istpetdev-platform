#!/usr/bin/env python3
import json
from pathlib import Path

def update_template(path: Path):
    with open(path, 'r', encoding='utf-8') as f:
        data = json.load(f)

    # 1. executionTimeout
    data['settings']['executionTimeout'] = 11

    # 2. Iterate nodes
    for node in data['nodes']:
        ntype = node.get('type', '')
        name = node.get('name', '')
        params = node.get('parameters', {})

        if ntype == '@n8n/n8n-nodes-langchain.agent':
            params['text'] = "={{ JSON.stringify($('Verify Signature').first().json.payload) }}"

        elif ntype == '@n8n/n8n-nodes-langchain.toolWorkflow':
            # Remove fields and workflowInputs
            params.pop('fields', None)
            params.pop('workflowInputs', None)

            # Define inputSchema
            if name == 'get_risks':
                schema = {
                    'type': 'object',
                    'properties': {
                        'runId': {'type': 'string'},
                        'kind': {'type': 'string'},
                        'apiBaseUrl': {'type': 'string'},
                        'deadlineUtc': {'type': 'string'}
                    },
                    'additionalProperties': True
                }
                incident_required = False
                req_url = "={{ ($json.apiBaseUrl || $env.ISTPETDEV_API_BASE_URL || 'http://host.docker.internal:5017/api/v1') + '/risks' }}"
            elif name == 'propose_routes':
                schema = {
                    'type': 'object',
                    'properties': {
                        'runId': {'type': 'string'},
                        'kind': {'type': 'string'},
                        'apiBaseUrl': {'type': 'string'},
                        'deadlineUtc': {'type': 'string'}
                    },
                    'additionalProperties': True
                }
                incident_required = False
                req_url = "={{ ($json.apiBaseUrl || $env.ISTPETDEV_API_BASE_URL || 'http://host.docker.internal:5017/api/v1') + '/route-plans' }}"
            elif name == 'get_route_plan':
                schema = {
                    'type': 'object',
                    'properties': {
                        'routePlanId': {'type': 'string', 'description': 'ID de una ruta devuelta por una herramienta autorizada.'},
                        'runId': {'type': 'string'},
                        'kind': {'type': 'string'},
                        'apiBaseUrl': {'type': 'string'},
                        'deadlineUtc': {'type': 'string'}
                    },
                    'required': ['routePlanId'],
                    'additionalProperties': True
                }
                incident_required = False
                req_url = "={{ ($json.apiBaseUrl || $env.ISTPETDEV_API_BASE_URL || 'http://host.docker.internal:5017/api/v1') + '/route-plans/' + encodeURIComponent($json.routePlanId || ($json.query || {}).routePlanId || '') }}"
            elif name == 'get_incident_impact':
                schema = {
                    'type': 'object',
                    'properties': {
                        'incidentId': {'type': 'string', 'description': 'ID del incidente recibido en el disparo.'},
                        'runId': {'type': 'string'},
                        'kind': {'type': 'string'},
                        'apiBaseUrl': {'type': 'string'},
                        'deadlineUtc': {'type': 'string'}
                    },
                    'additionalProperties': True
                }
                incident_required = True
                req_url = "={{ ($json.apiBaseUrl || $env.ISTPETDEV_API_BASE_URL || 'http://host.docker.internal:5017/api/v1') + '/incidents/' + encodeURIComponent($json.incidentId || ($json.query || {}).incidentId || '') }}"
            elif name == 'replan':
                schema = {
                    'type': 'object',
                    'properties': {
                        'routePlanId': {'type': 'string', 'description': 'ID de la ruta a replanificar devuelta por el impacto del incidente.'},
                        'incidentId': {'type': 'string', 'description': 'ID del incidente recibido en el disparo.'},
                        'runId': {'type': 'string'},
                        'kind': {'type': 'string'},
                        'apiBaseUrl': {'type': 'string'},
                        'deadlineUtc': {'type': 'string'}
                    },
                    'required': ['routePlanId'],
                    'additionalProperties': True
                }
                incident_required = True
                req_url = "={{ ($json.apiBaseUrl || $env.ISTPETDEV_API_BASE_URL || 'http://host.docker.internal:5017/api/v1') + '/route-plans/' + encodeURIComponent($json.routePlanId || ($json.query || {}).routePlanId || '') + '/replan' }}"
            else:
                raise ValueError(f'Unknown tool: {name}')

            params['inputSchema'] = json.dumps(schema, separators=(',', ':'))

            # Update subworkflow
            sub_wf = json.loads(params['workflowJson'])
            for sub_node in sub_wf['nodes']:
                if sub_node['name'] == 'Check Deadline':
                    sub_node['parameters']['jsCode'] = (
                        "const input = $input.item.json || {};\n"
                        "let parsedQuery = {};\n"
                        "if (input.query) {\n"
                        "  if (typeof input.query === 'object') {\n"
                        "    parsedQuery = input.query;\n"
                        "  } else if (typeof input.query === 'string') {\n"
                        "    try {\n"
                        "      parsedQuery = JSON.parse(input.query);\n"
                        "    } catch (e) {\n"
                        "      parsedQuery = { value: input.query };\n"
                        "    }\n"
                        "  }\n"
                        "}\n"
                        "const routePlanId = input.routePlanId || parsedQuery.routePlanId || (input.query && input.query.routePlanId) || parsedQuery.value || (typeof input.query === 'string' && input.query.trim().length === 36 ? input.query.trim() : '');\n"
                        "const runId = input.runId || parsedQuery.runId || (input.query && input.query.runId) || '';\n"
                        "const incidentId = input.incidentId || parsedQuery.incidentId || (input.query && input.query.incidentId) || '';\n"
                        "const kind = input.kind || parsedQuery.kind || (input.query && input.query.kind) || (incidentId ? 'Incident' : 'Watch');\n"
                        "const apiBaseUrl = input.apiBaseUrl || parsedQuery.apiBaseUrl || (input.query && input.query.apiBaseUrl) || $env.ISTPETDEV_API_BASE_URL || 'http://host.docker.internal:5017/api/v1';\n"
                        "const deadlineUtc = input.deadlineUtc || parsedQuery.deadlineUtc || (input.query && input.query.deadlineUtc);\n"
                        "const remaining = deadlineUtc ? Date.parse(deadlineUtc) - Date.now() : 8000;\n"
                        f"const incidentRequired = {'true' if incident_required else 'false'};\n"
                        "const validIncident = !incidentRequired || Boolean(incidentId);\n"
                        "const canCall = Number.isFinite(remaining) && remaining >= 3200 && validIncident;\n"
                        "const skipReason = !validIncident ? 'incident_context_required' : 'deadline_too_short';\n"
                        "return { json: { ...input, ...parsedQuery, routePlanId, runId, kind, incidentId, apiBaseUrl, deadlineUtc, canCall, skipReason } };"
                    )
                elif sub_node['name'] == 'HTTP Request':
                    sub_node['parameters']['url'] = req_url
                    headers = sub_node['parameters'].setdefault('headerParameters', {}).setdefault('parameters', [])
                    found_header = False
                    for h in headers:
                        if h.get('name') == 'X-Agent-Run-Id':
                            h['value'] = "={{ $json.runId || ($json.query || {}).runId || '' }}"
                            found_header = True
                    if not found_header:
                        headers.append({'name': 'X-Agent-Run-Id', 'value': "={{ $json.runId || ($json.query || {}).runId || '' }}"})

                    if name == 'replan':
                        sub_node['parameters']['jsonBody'] = "={{ JSON.stringify({ incidentId: $json.incidentId || ($json.query || {}).incidentId || '' }) }}"

            params['workflowJson'] = json.dumps(sub_wf)

        elif name == 'Send Result':
            params['url'] = "={{ ($('Verify Signature').first().json.payload.apiBaseUrl || $env.ISTPETDEV_API_BASE_URL || 'http://host.docker.internal:5017/api/v1') + '/agent/runs/' + $('Verify Signature').first().json.payload.runId + '/result' }}"

    with open(path, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
        f.write('\n')
    print('Updated', path)

update_template(Path('automation/n8n/templates/vigia.template.json'))
update_template(Path('automation/n8n/templates/incidente.template.json'))
