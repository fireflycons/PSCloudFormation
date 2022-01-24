/*

    Replacement main() function for terraform-proovider-aws

    It does the following:
    - Loads the provider object which gathers all the resource schema
	- Copies the schema into a simpler object graph to drop all the function pointer members
	- Dumps the schema to JSON
	- Dumps the resource type names to a text file for the PowerShell script to perform resource-type match with AWS resource types

*/package main

import (
	"encoding/json"
	"fmt"
	"os"
	"path"

	"github.com/hashicorp/terraform-plugin-sdk/v2/helper/schema"
	"github.com/hashicorp/terraform-provider-aws/internal/provider"
)

// Simplified resource object containing only the resource schema
type SimpleResource struct {
	Schema map[string]*SimpleSchema
}

// Simplified schema object containing only the fields we need.
type SimpleSchema struct {
	Type         schema.ValueType
	ConfigMode   schema.SchemaConfigMode
	Optional     bool        `json:",omitempty"`
	Required     bool        `json:",omitempty"`
	Default      interface{} `json:",omitempty"`
	InputDefault string      `json:",omitempty"`
	Computed     bool        `json:",omitempty"`
	Elem         interface{} `json:",omitempty"`
}

func recurseSchema(elem interface{}) interface{} {

	switch v := elem.(type) {

	case *schema.Resource:
		return copyResource(v)

	case *schema.Schema:
		return copySchema(v)
	}

	return nil
}

func copySchema(s *schema.Schema) *SimpleSchema {

	return &SimpleSchema{
		Type:         s.Type,
		ConfigMode:   s.ConfigMode,
		Optional:     s.Optional,
		Required:     s.Required,
		Default:      s.Default,
		InputDefault: s.InputDefault,
		Computed:     s.Computed,
		Elem:         recurseSchema(s.Elem),
	}
}

func copyResource(r *schema.Resource) *SimpleResource {

	rs := make(map[string]*SimpleSchema)
	for k, s := range r.Schema {
		rs[k] = copySchema(s)
	}

	return &SimpleResource{
		Schema: rs,
	}
}

// Requires arguments (in order)
// Directory in which to write the resource schema
// Directory in which to write the resource type names
func main() {

	if len(os.Args) < 3 {
		usage(os.Args)
	}

	schemaDirname := os.Args[1]
	resourceNamesDirName := os.Args[2]

	checkDirectory(schemaDirname)
	checkDirectory(resourceNamesDirName)

	schemaOutFile, err := os.Create(path.Join(schemaDirname, "terraform-aws-schema.json"))
	if err != nil {
		panic(err)
	}

	resourceNamesOutFile, err := os.Create(path.Join(resourceNamesDirName, "terraform-aws-resource-names.txt"))
	if err != nil {
		panic(err)
	}

	defer func() {
		if err := schemaOutFile.Close(); err != nil {
			panic(err)
		}
		if err := resourceNamesOutFile.Close(); err != nil {
			panic(err)
		}
	}()

	fmt.Println("Exporting terraform-aws-provider schema...")
	simpleResourceMap := make(map[string]*SimpleResource)
	count := 0

	for k, r := range provider.Provider().ResourcesMap {
		simpleResourceMap[k] = copyResource(r)
		resourceNamesOutFile.WriteString(k + "\n")
		count++
	}

	j, e := json.MarshalIndent(simpleResourceMap, "", "  ")

	if e != nil {
		fmt.Printf("JSON error: %s\n", e.Error())
	} else {
		schemaOutFile.Write(j)
	}

	fmt.Printf("%d resources exported.\n\n", count)
}

func checkDirectory(dir string) {
	fi, err := os.Stat(dir)

	if os.IsNotExist(err) {
		panic(fmt.Sprintf("Directory '%s' not found", dir))
	}

	if !fi.IsDir() {
		panic(fmt.Sprintf("'%s' is not a directory", dir))
	}
}

func usage(args []string) {
	fmt.Printf("Usage: %s dir_for_schema dir_for_resource_list", args[0])
	panic("Missing command line arguments")
}
