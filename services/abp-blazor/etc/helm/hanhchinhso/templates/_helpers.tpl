{{- define "hanhchinhso.hosts.authserver" -}}
{{- print "https://" (.Values.global.hosts.authserver | replace "[RELEASE_NAME]" .Release.Name) -}}
{{- end -}}
{{- define "hanhchinhso.hosts.webgateway" -}}
{{- print "https://" (.Values.global.hosts.webgateway | replace "[RELEASE_NAME]" .Release.Name) -}}
{{- end -}}
{{- define "hanhchinhso.hosts.kibana" -}}
{{- print "https://" (.Values.global.hosts.kibana | replace "[RELEASE_NAME]" .Release.Name) -}}
{{- end -}}
{{- define "hanhchinhso.hosts.prometheus" -}}
{{- print "https://" (.Values.global.hosts.prometheus | replace "[RELEASE_NAME]" .Release.Name) -}}
{{- end -}}
{{- define "hanhchinhso.hosts.grafana" -}}
{{- print "https://" (.Values.global.hosts.grafana | replace "[RELEASE_NAME]" .Release.Name) -}}
{{- end -}}
{{- define "hanhchinhso.hosts.blazorwebapp" -}}
{{- print "https://" (.Values.global.hosts.blazorwebapp | replace "[RELEASE_NAME]" .Release.Name) -}}
{{- end -}}
