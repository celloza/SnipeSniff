# SnipeSniff
Network sniffer that creates and updates assets in Snipe-IT. Created from a fork of https://github.com/Scope-IT/marksman.

# Server or Agent mode
This solution makes provision for either installing it on all the machines to be inventoried in Agent mode, or by running the solution in Service mode that sniffs the provided subnets for machines, and populates them in Snipe-IT.

Makes use of the [Microsoft.Management.Infrastructure](https://docs.microsoft.com/en-us/previous-versions/windows/desktop/wmi_v2/mi-managed-api/hh832958(v=vs.85)) namespace to query information about machines.
