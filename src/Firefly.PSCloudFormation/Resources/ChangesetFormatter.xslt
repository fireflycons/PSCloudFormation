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
                <script type="text/javascript" src="https://rawgit.com/DanielHoffmann/jquery-svg-pan-zoom/master/compiled/jquery.svg.pan.zoom.js"></script>
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
                    color: #ffc107;
                    }

                    .red {
                    color: red;
                    }

                    .cyan {
                    color: cyan;
                    }

                    .green {
                    color: #34d659;
                    }

                    .left {
                    width: 150px;
                    }

                    .detail-btn, graph-btn {
                    width: 120px;
                    }
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="row">
                        <div class="col">
                            <h3>Detailed Changeset Information</h3>
                        </div>
                    </div>
                    <xsl:for-each select="Stack">
                        <div class="mb-4">
                            <div class="row">
                                <div class="col">
                                    <div class="card text-white bg-info mb-2">
                                        <div class="card-header"><h6>Stack: <xsl:value-of select="StackName"/></h6></div>
                                        <div class="card-body">
                                            <table class="vert">
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
                                            <xsl:if test="Graph">
                                                <a class="btn btn-primary btn-sm graph-btn mt-2" data-toggle="collapse" href="#{generate-id(StackName)}" role="button" aria-expanded="false" aria-controls="{generate-id(StackName)}">Show Graph</a>
                                            </xsl:if>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <xsl:if test="Graph">
                                <div class="row">
                                    <div class="col">
                                        <div class="collapse multi-collapse" id="{generate-id(StackName)}">
                                            <div class="card card-body bg-dark mb-1 text-center">
                                                <xsl:for-each select="Graph">
                                                    <xsl:copy-of select="*"/>
                                                </xsl:for-each>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </xsl:if>
                            <xsl:for-each select="Changes">
                                <div class="row">
                                    <div class="col">
                                        <div class="card card-body bg-dark mb-1">
                                            <table class="vert mb-3">
                                                <tr>
                                                    <td class="left">Action</td>
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
                                                </tr>
                                                <tr>
                                                    <td class="left">Logical Id</td>
                                                    <td>
                                                        <xsl:value-of select="ResourceChange/LogicalResourceId"/>
                                                    </td>
                                                </tr>
                                                <tr>
                                                    <td class="left">Type</td>
                                                    <td>
                                                        <xsl:value-of select="ResourceChange/ResourceType"/>
                                                    </td>
                                                </tr>
                                                <xsl:if test="ResourceChange/Action != 'Add'">
                                                    <xsl:if test="ResourceChange/Action != 'Remove'">
                                                        <tr>
                                                            <td class="left">Replacement</td>
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
                                                        </tr>
                                                        <tr>
                                                            <td class="left">Scope</td>
                                                            <td>
                                                                <xsl:for-each select="ResourceChange/Scope">
                                                                    <xsl:if test="not(position() = 1)">, </xsl:if>
                                                                    <xsl:value-of select="."/>
                                                                </xsl:for-each>
                                                            </td>
                                                        </tr>
                                                    </xsl:if>
                                                    <tr>
                                                        <td class="left">Physical ID</td>
                                                        <td>
                                                            <xsl:value-of select="ResourceChange/PhysicalResourceId"/>
                                                        </td>
                                                    </tr>
                                                </xsl:if>
                                            </table>
                                            <xsl:if test="(ResourceChange/Action = 'Modify') or (ResourceChange/ModuleInfo != '')">
                                                <a class="btn btn-primary btn-sm detail-btn mt-2" data-toggle="collapse" href="#{generate-id(ResourceChange/LogicalResourceId)}" role="button" aria-expanded="false" aria-controls="{generate-id(ResourceChange/LogicalResourceId)}">Show Detail</a>
                                            </xsl:if>
                                        </div>
                                    </div>
                                </div>
                                <xsl:if test="(ResourceChange/Action = 'Modify') or (ResourceChange/ModuleInfo != '')">
                                    <div class="row">
                                        <div class="col">
                                            <div class="collapse multi-collapse" id="{generate-id(ResourceChange/LogicalResourceId)}">
                                                <div class="card card-body bg-dark mb-1">
                                                    <h5>Additional Properties</h5>
                                                    <table class="vert mb-3">
                                                        <tr>
                                                            <td class="left">Type</td>
                                                            <td>
                                                                <xsl:value-of select="Type"/>
                                                            </td>
                                                        </tr>
                                                        <xsl:if test="ResourceChange/ChangeSetId != ''">
                                                            <tr>
                                                                <td class="left">Changeset ID</td>
                                                                <td>
                                                                    <xsl:value-of select="ResourceChange/ChangeSetId"/>
                                                                </td>
                                                            </tr>
                                                        </xsl:if>
                                                        <xsl:if test="ResourceChange/ModuleInfo != ''">
                                                            <tr>
                                                                <td class="left">Module Info</td>
                                                                <td>
                                                                    <table>
                                                                        <tr>
                                                                            <td>Logical ID Hierarchy</td>
                                                                            <td>
                                                                                <xsl:value-of select="ResourceChangeModuleInfo/LogicalIdHierarchy"/>
                                                                            </td>
                                                                        </tr>
                                                                        <tr>
                                                                            <td>Type Hierarchy</td>
                                                                            <td>
                                                                                <xsl:value-of select="ResourceChange/ModuleInfo/TypeHierarchy"/>
                                                                            </td>
                                                                        </tr>
                                                                    </table>
                                                                </td>
                                                            </tr>
                                                        </xsl:if>
                                                    </table>
                                                    <xsl:if test="ResourceChange/Scope != ''">
                                                        <h5>Scope</h5>
                                                        <ul>
                                                            <xsl:for-each select="ResourceChange/Scope">
                                                                <li>
                                                                    <xsl:value-of select="."/>
                                                                </li>
                                                            </xsl:for-each>
                                                        </ul>
                                                    </xsl:if>
                                                    <h5>Change Detail</h5>
                                                    <ol>
                                                        <xsl:for-each select="ResourceChange/Details">
                                                            <li>
                                                                <span>
                                                                    <h6>Source</h6>
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
                                                                    <h6>Target</h6>
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
                                </xsl:if>
                            </xsl:for-each>
                        </div>
                    </xsl:for-each>
                </div>
                <script language="JavaScript">
                    $(document).ready(function () {
                    var graphs = $("svg").svgPanZoom();
                    $('.detail-btn').on('click', function () {
                    var text = $(this).text();
                    if (text === "Show Detail") {
                    $(this).html('Hide Detail');
                    } else {
                    $(this).text('Show Detail');
                    }
                    });
                    $('.graph-btn').on('click', function () {
                    var text = $(this).text();
                    if (text === "Show Graph") {
                    $(this).html('Hide Graph');
                    } else {
                    $(this).text('Show Graph');
                    }
                    });
                    });
                </script>
            </body>
        </html>
    </xsl:template>
</xsl:stylesheet>
