#!/bin/bash

# Remove sensitive information from Git history
git filter-branch --force --index-filter \
"git rm --cached --ignore-unmatch setup_ranking_db.sql update_table.sql" \
--prune-empty --tag-name-filter cat -- --all

# Force push the changes
git push origin --force --all 