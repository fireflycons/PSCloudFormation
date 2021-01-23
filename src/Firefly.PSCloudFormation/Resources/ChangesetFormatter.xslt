<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
>
    <xsl:template match="/Stacks">
        <html>
            <head>
                <title>Changeset Detail</title>
                <link rel="stylesheet" href="https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/css/bootstrap.min.css" integrity="sha384-JcKb8q3iqJ61gNV9KGb8thSsNjpSL0n8PARn9HuZOnIxN0hoP+VmmDGMN5t9UJ0Z" crossorigin="anonymous"></link>
                <script src="https://code.jquery.com/jquery-3.5.1.min.js" integrity="sha256-9/aliU8dGd2tb6OSsuzixeV4y/faTqgFtohetphbbj0=" crossorigin="anonymous"></script>
                <script src="https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.min.js" integrity="sha384-B4gt1jrGC7Jh4AgTPSdUtOBvfO8shuf57BaghqFfPlYxofvL8/KUEfYiJOMMV+rV" crossorigin="anonymous"></script>

                <style>
                    body {
                    font-family: arial, sans-serif;
                    font-size: 0.75rem;
                    color: white;
                    background-color: black;
                    }

                    table {
                    font-size: 0.75rem;
                    }

                    .change-summary {
                    border: 1px solid #dddddd;
                    border-collapse: collapse;
                    width: 100%;
                    }

                    .vert {
                    font-family: arial, sans-serif;
                    border-collapse: collapse;
                    }

                    /*
                    td, th {
                    text-align: left;
                    padding: 4px;
                    }*/
                    .yellow {
                    color: yellow;
                    }

                    .red {
                    color: red;
                    }

                    .cyan {
                    color: cyan;
                    }

                    .green {
                    color: lightgreen;
                    }

                    .left {
                    width: 150px;
                    }

                    .detail-btn {
                    width: 120px;
                    }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="row">
                        <div class="col">
                            <h1>Detailed Changeset Information</h1>
                        </div>
                    </div>
                    <xsl:for-each select="Stack">
                        <div class="row">
                            <div class="col">
                                <table class="vert mb-3">
                                    <tr>
                                        <td class="left">Stack Name</td>
                                        <td>
                                            <xsl:value-of select="StackName"/>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td class="left">Changeset Name</td>
                                        <td>
                                            <xsl:value-of select="ChangeSetName"/>
                                        </td>
                                    </tr>
                                    <xsl:if test="Description != ''">
                                        <tr>
                                            <td class="left">Description</td>
                                            <td>
                                                <xsl:value-of select="Description"/>
                                            </td>
                                        </tr>
                                    </xsl:if>
                                    <tr>
                                        <td class="left">CreationTime</td>
                                        <td>
                                            <xsl:value-of select="CreationTime"/>
                                        </td>
                                    </tr>
                                </table>
                            </div>
                        </div>
                        <xsl:for-each select="Changes">
                            <div class="row">
                                <div class="col">
                                    <div class="card card-body bg-dark mb-1">
                                        <table class="change-summary">
                                            <th>Action</th>
                                            <th>Logical Id</th>
                                            <th>Type</th>
                                            <th>Replacement</th>
                                            <th>Physical ID</th>
                                            <tr>
                                                <xsl:choose>
                                                    <xsl:when test="ResourceChange/Action = 'Add'">
                                                        <td class="green">
                                                            <xsl:value-of select="ResourceChange/Action"/>
                                                        </td>
                                                    </xsl:when>
                                                    <xsl:when test="ResourceChange/Action = 'Modify'">
                                                        <td class="yellow">
                                                            <xsl:value-of select="ResourceChange/Action"/>
                                                        </td>
                                                    </xsl:when>
                                                    <xsl:when test="ResourceChange/Action = 'Remove'">
                                                        <td class="Red">
                                                            <xsl:value-of select="ResourceChange/Action"/>
                                                        </td>
                                                    </xsl:when>
                                                    <xsl:when test="ResourceChange/Action = 'Import'">
                                                        <td class="Cyan">
                                                            <xsl:value-of select="ResourceChange/Action"/>
                                                        </td>
                                                    </xsl:when>
                                                    <xsl:otherwise>
                                                        <td>
                                                            <xsl:value-of select="ResourceChange/Action"/>
                                                        </td>
                                                    </xsl:otherwise>
                                                </xsl:choose>
                                                <td>
                                                    <xsl:value-of select="ResourceChange/LogicalResourceId"/>
                                                </td>
                                                <td>
                                                    <xsl:value-of select="ResourceChange/ResourceType"/>
                                                </td>
                                                <xsl:choose>
                                                    <xsl:when test="ResourceChange/Replacement = 'False'">
                                                        <td class="green">
                                                            <xsl:value-of select="ResourceChange/Replacement"/>
                                                        </td>
                                                    </xsl:when>
                                                    <xsl:when test="ResourceChange/Replacement = 'Conditional'">
                                                        <td class="yellow">
                                                            <xsl:value-of select="ResourceChange/Replacement"/>
                                                        </td>
                                                    </xsl:when>
                                                    <xsl:when test="ResourceChange/Replacement = 'True'">
                                                        <td class="Red">
                                                            <xsl:value-of select="ResourceChange/Replacement"/>
                                                        </td>
                                                    </xsl:when>
                                                    <xsl:otherwise>
                                                        <td>
                                                            <xsl:value-of select="ResourceChange/Replacement"/>
                                                        </td>
                                                    </xsl:otherwise>
                                                </xsl:choose>
                                                <td>
                                                    <xsl:value-of select="ResourceChange/PhysicalResourceId"/>
                                                </td>
                                            </tr>
                                        </table>
                                        <a class="btn btn-primary btn-sm detail-btn mt-2" data-toggle="collapse" href="#{generate-id(ResourceChange/LogicalResourceId)}" role="button" aria-expanded="false" aria-controls="{generate-id(ResourceChange/LogicalResourceId)}">Show Detail</a>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col">
                                    <div class="collapse multi-collapse" id="{generate-id(ResourceChange/LogicalResourceId)}">
                                        <div class="card card-body bg-dark mb-3">
                                            <xsl:if test="ResourceChange/Scope != ''">
                                                <h4>Scope</h4>
                                                <ul>
                                                    <xsl:choose>
                                                        <xsl:when test="ResourceChange/Scope/Item != ''">
                                                            <xsl:for-each select="ResourceChange/Scope/Item">
                                                                <li>
                                                                    <xsl:value-of select="."/>
                                                                </li>
                                                            </xsl:for-each>
                                                        </xsl:when>
                                                        <xsl:otherwise>
                                                            <li>
                                                                <xsl:value-of select="ResourceChange/Scope"/>
                                                            </li>
                                                        </xsl:otherwise>
                                                    </xsl:choose>
                                                </ul>
                                            </xsl:if>
                                            <h4>Change Detail</h4>
                                            <ol>
                                                <xsl:for-each select="ResourceChange/Details">
                                                    <li>
                                                        <span>
                                                            <h5>Source</h5>
                                                            <table class="vert mb-3">
                                                                <xsl:if test="CausingEntity != ''">
                                                                    <tr>
                                                                        <td class="left">Causing Entity</td>
                                                                        <td>
                                                                            <xsl:value-of select="CausingEntity"/>
                                                                        </td>
                                                                    </tr>
                                                                </xsl:if>
                                                                <tr>
                                                                    <td class="left">Change Source</td>
                                                                    <td>
                                                                        <xsl:value-of select="ChangeSource"/>
                                                                    </td>
                                                                </tr>
                                                                <tr>
                                                                    <td class="left">Evaluation</td>
                                                                    <td>
                                                                        <xsl:value-of select="Evaluation"/>
                                                                    </td>
                                                                </tr>
                                                            </table>
                                                            <h5>Target</h5>
                                                            <table class="vert mb-3">
                                                                <tr>
                                                                    <td class="left">Attribute</td>
                                                                    <td>
                                                                        <xsl:value-of select="Target/Attribute"/>
                                                                    </td>
                                                                </tr>
                                                                <xsl:if test="Target/Name != ''">
                                                                    <tr>
                                                                        <td class="left">Name</td>
                                                                        <td>
                                                                            <xsl:value-of select="Target/Name"/>
                                                                        </td>
                                                                    </tr>
                                                                </xsl:if>
                                                                <tr>
                                                                    <td class="left">Requires Recreation</td>
                                                                    <xsl:choose>
                                                                        <xsl:when test="Target/RequiresRecreation = 'Never'">
                                                                            <td class="green">
                                                                                <xsl:value-of select="Target/RequiresRecreation"/>
                                                                            </td>
                                                                        </xsl:when>
                                                                        <xsl:when test="Target/RequiresRecreation = 'Conditionally'">
                                                                            <td class="yellow">
                                                                                <xsl:value-of select="Target/RequiresRecreation"/>
                                                                            </td>
                                                                        </xsl:when>
                                                                        <xsl:when test="Target/RequiresRecreation = 'Always'">
                                                                            <td class="Red">
                                                                                <xsl:value-of select="Target/RequiresRecreation"/>
                                                                            </td>
                                                                        </xsl:when>
                                                                        <xsl:otherwise>
                                                                            <td>
                                                                                <xsl:value-of select="Target/RequiresRecreation"/>
                                                                            </td>
                                                                        </xsl:otherwise>
                                                                    </xsl:choose>
                                                                </tr>
                                                            </table>
                                                        </span>
                                                    </li>
                                                </xsl:for-each>
                                            </ol>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </xsl:for-each>
                    </xsl:for-each>
                </div>
                <script language="JavaScript">
                    $(document).ready(function() {
                        $('.detail-btn').on('click', function() {
                            var text = $(this).text();
                            if (text === "Show Detail") {
                                $(this).html('Hide Detail');
                            } else {
                                $(this).text('Show Detail');
                            }
                        });
                    });
                </script>
            </body>
        </html>
    </xsl:template>
</xsl:stylesheet>
