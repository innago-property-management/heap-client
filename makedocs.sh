#! /usr/bin/env bash

__command="$HOME/repos/XmlDocMarkdown/src/xmldocmd/bin/Debug/net9.0/xmldocmd"

installCount=$(dotnet tool list dotnet-outdated-tool --global --format json | jq '.data | length')
if [[ $installCount -lt 0 ]]; then
  dotnet tool install xmldocmd -g
fi 

projects=$(find . -name "*.csproj" -path "*/src/*")
for project in $projects; do 
  dir=$(dirname "$project")
  pushd "$dir" > /dev/null 2>&1 || continue 
  
  project_file=$(basename "$project")
  
  doc_file=$(xq --raw-output '.Project.PropertyGroup[] | .DocumentationFile? | select(. != null)' "$project_file" | tr '\' '/')
  
  [[ -z $doc_file ]] && popd > /dev/null 2>&1 && continue
  
  doc_dir=$(dirname "$doc_file")
  
  assembly_name=$(xq --raw-output '.Project.PropertyGroup[] | .AssemblyName? | select(. != null)' "$project_file")
  
  if [[ -z $assembly_name ]]; then
    assembly_name=$(basename "$project_file" .csproj)
  fi
  
  echo "$assembly_name"
  
  "$__command" "$doc_dir/$assembly_name.dll" \
    --source . \
    --visibility public \
    --skip-unbrowsable \
    --skip-compiler-generated \
    --clean \
    docs/
    
    printf "wrote docs for %s\n" "$assembly_name"

  popd > /dev/null 2>&1 || return 
done
