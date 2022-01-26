using System;
using System.Collections.Generic;
using System.Text;

namespace Firefly.PSCloudFormation.Tests.Unit
{
    using Firefly.PSCloudFormation.Terraform.HclSerializer;
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Events;
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Schema;

    internal static class EventQueues
    {
        internal static ResourceSchema Ec2InstanceSchema = AwsSchema.GetResourceSchema("aws_instance");

        internal static ResourceSchema AutoScalingGroupSchema = AwsSchema.GetResourceSchema("aws_autoscaling_group");

        internal static HclEvent[] Ec2Events =
            {
                new ResourceStart("aws_instance", "my_instance"), 
                new MappingStart(),
                new MappingKey("ami", new AttributePath("ami"), Ec2InstanceSchema.GetAttributeByPath("ami")),
                new ScalarValue("var.AmiId"),
                new MappingKey("arn", new AttributePath("arn"), Ec2InstanceSchema.GetAttributeByPath("arn")),
                new ScalarValue("arn:aws:ec2:eu-west-1:123456789012:instance/i-05c2ec6ce1d453b0c"),
                new MappingKey(
                    "associate_public_ip_address",
                    new AttributePath("associate_public_ip_address"),
                    Ec2InstanceSchema.GetAttributeByPath("associate_public_ip_address")),
                new ScalarValue("false"),
                new MappingKey(
                    "availability_zone",
                    new AttributePath("availability_zone"),
                    Ec2InstanceSchema.GetAttributeByPath("availability_zone")),
                new ScalarValue("eu-west-1a"),
                new MappingKey(
                    "capacity_reservation_specification",
                    new AttributePath("capacity_reservation_specification"),
                    Ec2InstanceSchema.GetAttributeByPath("capacity_reservation_specification")),
                new SequenceStart(), 
                new MappingStart(),
                new MappingKey(
                    "capacity_reservation_preference",
                    new AttributePath("capacity_reservation_specification.0.capacity_reservation_preference"),
                    Ec2InstanceSchema.GetAttributeByPath(
                        "capacity_reservation_specification.0.capacity_reservation_preference")),
                new ScalarValue("open"),
                new MappingKey(
                    "capacity_reservation_target",
                    new AttributePath("capacity_reservation_specification.0.capacity_reservation_target"),
                    Ec2InstanceSchema.GetAttributeByPath(
                        "capacity_reservation_specification.0.capacity_reservation_target")),
                new SequenceStart(), 
                new SequenceEnd(), 
                new MappingEnd(), 
                new SequenceEnd(),
                new MappingKey(
                    "cpu_core_count",
                    new AttributePath("cpu_core_count"),
                    Ec2InstanceSchema.GetAttributeByPath("cpu_core_count")),
                new ScalarValue("1"),
                new MappingKey(
                    "cpu_threads_per_core",
                    new AttributePath("cpu_threads_per_core"),
                    Ec2InstanceSchema.GetAttributeByPath("cpu_threads_per_core")),
                new ScalarValue("2"),
                new MappingKey(
                    "credit_specification",
                    new AttributePath("credit_specification"),
                    Ec2InstanceSchema.GetAttributeByPath("credit_specification")),
                new SequenceStart(), 
                new MappingStart(),
                new MappingKey(
                    "cpu_credits",
                    new AttributePath("credit_specification.0.cpu_credits"),
                    Ec2InstanceSchema.GetAttributeByPath("credit_specification.0.cpu_credits")),
                new ScalarValue("unlimited"),
                new MappingEnd(), 
                new SequenceEnd(),
                new MappingKey(
                    "disable_api_termination",
                    new AttributePath("disable_api_termination"),
                    Ec2InstanceSchema.GetAttributeByPath("disable_api_termination")),
                new ScalarValue("false"),
                new MappingKey(
                    "ebs_block_device",
                    new AttributePath("ebs_block_device"),
                    Ec2InstanceSchema.GetAttributeByPath("ebs_block_device")),
                new SequenceStart(), 
                new SequenceEnd(),
                new MappingKey(
                    "ebs_optimized",
                    new AttributePath("ebs_optimized"),
                    Ec2InstanceSchema.GetAttributeByPath("ebs_optimized")),
                new ScalarValue("false"),
                new MappingKey(
                    "enclave_options",
                    new AttributePath("enclave_options"),
                    Ec2InstanceSchema.GetAttributeByPath("enclave_options")),
                new SequenceStart(), 
                new MappingStart(),
                new MappingKey(
                    "enabled",
                    new AttributePath("enclave_options.0.enabled"),
                    Ec2InstanceSchema.GetAttributeByPath("enclave_options.0.enabled")),
                new ScalarValue("false"), 
                new MappingEnd(), 
                new SequenceEnd(),
                new MappingKey(
                    "ephemeral_block_device",
                    new AttributePath("ephemeral_block_device"),
                    Ec2InstanceSchema.GetAttributeByPath("ephemeral_block_device")),
                new SequenceStart(), 
                new SequenceEnd(),
                new MappingKey(
                    "get_password_data",
                    new AttributePath("get_password_data"),
                    Ec2InstanceSchema.GetAttributeByPath("get_password_data")),
                new ScalarValue("false"),
                new MappingKey(
                    "hibernation",
                    new AttributePath("hibernation"),
                    Ec2InstanceSchema.GetAttributeByPath("hibernation")),
                new ScalarValue("false"),
                new MappingKey("host_id", new AttributePath("host_id"), Ec2InstanceSchema.GetAttributeByPath("host_id")),
                new ScalarValue((string)null),
                new MappingKey(
                    "iam_instance_profile",
                    new AttributePath("iam_instance_profile"),
                    Ec2InstanceSchema.GetAttributeByPath("iam_instance_profile")),
                new ScalarValue("aws_iam_instance_profile.InstanceProfile.id"),
                new MappingKey("id", new AttributePath("id"), Ec2InstanceSchema.GetAttributeByPath("id")),
                new ScalarValue("i-05c2ec6ce1d453b0c"),
                new MappingKey(
                    "instance_initiated_shutdown_behavior",
                    new AttributePath("instance_initiated_shutdown_behavior"),
                    Ec2InstanceSchema.GetAttributeByPath("instance_initiated_shutdown_behavior")),
                new ScalarValue("stop"),
                new MappingKey(
                    "instance_state",
                    new AttributePath("instance_state"),
                    Ec2InstanceSchema.GetAttributeByPath("instance_state")),
                new ScalarValue("stopped"),
                new MappingKey(
                    "instance_type",
                    new AttributePath("instance_type"),
                    Ec2InstanceSchema.GetAttributeByPath("instance_type")),
                new ScalarValue("t3.medium"),
                new MappingKey(
                    "ipv6_address_count",
                    new AttributePath("ipv6_address_count"),
                    Ec2InstanceSchema.GetAttributeByPath("ipv6_address_count")),
                new ScalarValue("0"),
                new MappingKey(
                    "ipv6_addresses",
                    new AttributePath("ipv6_addresses"),
                    Ec2InstanceSchema.GetAttributeByPath("ipv6_addresses")),
                new SequenceStart(), 
                new SequenceEnd(),
                new MappingKey("key_name", new AttributePath("key_name"), Ec2InstanceSchema.GetAttributeByPath("key_name")),
                new ScalarValue("var.KeyPair"),
                new MappingKey(
                    "launch_template",
                    new AttributePath("launch_template"),
                    Ec2InstanceSchema.GetAttributeByPath("launch_template")),
                new SequenceStart(),
                new SequenceEnd(),
                new MappingKey(
                    "metadata_options",
                    new AttributePath("metadata_options"),
                    Ec2InstanceSchema.GetAttributeByPath("metadata_options")),
                new SequenceStart(),
                new MappingStart(),
                new MappingKey(
                    "http_endpoint",
                    new AttributePath("metadata_options.0.http_endpoint"),
                    Ec2InstanceSchema.GetAttributeByPath("metadata_options.0.http_endpoint")),
                new ScalarValue("enabled"),
                new MappingKey(
                    "http_put_response_hop_limit",
                    new AttributePath("metadata_options.0.http_put_response_hop_limit"),
                    Ec2InstanceSchema.GetAttributeByPath("metadata_options.0.http_put_response_hop_limit")),
                new ScalarValue("1"),
                new MappingKey(
                    "http_tokens",
                    new AttributePath("metadata_options.0.http_tokens"),
                    Ec2InstanceSchema.GetAttributeByPath("metadata_options.0.http_tokens")),
                new ScalarValue("optional"),
                new MappingEnd(), 
                new SequenceEnd(),
                new MappingKey(
                    "monitoring",
                    new AttributePath("monitoring"),
                    Ec2InstanceSchema.GetAttributeByPath("monitoring")),
                new ScalarValue("false"),
                new MappingKey(
                    "network_interface",
                    new AttributePath("network_interface"),
                    Ec2InstanceSchema.GetAttributeByPath("network_interface")),
                new SequenceStart(), 
                new SequenceEnd(),
                new MappingKey(
                    "outpost_arn",
                    new AttributePath("outpost_arn"),
                    Ec2InstanceSchema.GetAttributeByPath("outpost_arn")),
                new ScalarValue(string.Empty),
                new MappingKey(
                    "password_data",
                    new AttributePath("password_data"),
                    Ec2InstanceSchema.GetAttributeByPath("password_data")),
                new ScalarValue(string.Empty),
                new MappingKey(
                    "placement_group",
                    new AttributePath("placement_group"),
                    Ec2InstanceSchema.GetAttributeByPath("placement_group")),
                new ScalarValue(string.Empty),
                new MappingKey(
                    "placement_partition_number",
                    new AttributePath("placement_partition_number"),
                    Ec2InstanceSchema.GetAttributeByPath("placement_partition_number")),
                new ScalarValue((string)null),
                new MappingKey(
                    "primary_network_interface_id",
                    new AttributePath("primary_network_interface_id"),
                    Ec2InstanceSchema.GetAttributeByPath("primary_network_interface_id")),
                new ScalarValue("eni-0986ec5de5cdd445e"),
                new MappingKey(
                    "private_dns",
                    new AttributePath("private_dns"),
                    Ec2InstanceSchema.GetAttributeByPath("private_dns")),
                new ScalarValue("ip-10-96-0-24.eu-west-1.compute.internal"),
                new MappingKey(
                    "private_ip",
                    new AttributePath("private_ip"),
                    Ec2InstanceSchema.GetAttributeByPath("private_ip")),
                new ScalarValue("10.96.0.24"),
                new MappingKey(
                    "public_dns",
                    new AttributePath("public_dns"),
                    Ec2InstanceSchema.GetAttributeByPath("public_dns")),
                new ScalarValue(string.Empty),
                new MappingKey(
                    "public_ip",
                    new AttributePath("public_ip"),
                    Ec2InstanceSchema.GetAttributeByPath("public_ip")),
                new ScalarValue(string.Empty),
                new MappingKey(
                    "root_block_device",
                    new AttributePath("root_block_device"),
                    Ec2InstanceSchema.GetAttributeByPath("root_block_device")),
                new SequenceStart(), 
                new MappingStart(),
                new MappingKey(
                    "delete_on_termination",
                    new AttributePath("root_block_device.0.delete_on_termination"),
                    Ec2InstanceSchema.GetAttributeByPath("root_block_device.0.delete_on_termination")),
                new ScalarValue("true"),
                new MappingKey(
                    "device_name",
                    new AttributePath("root_block_device.0.device_name"),
                    Ec2InstanceSchema.GetAttributeByPath("root_block_device.0.device_name")),
                new ScalarValue("/dev/sda1"),
                new MappingKey(
                    "encrypted",
                    new AttributePath("root_block_device.0.encrypted"),
                    Ec2InstanceSchema.GetAttributeByPath("root_block_device.0.encrypted")),
                new ScalarValue("false"),
                new MappingKey(
                    "iops",
                    new AttributePath("root_block_device.0.iops"),
                    Ec2InstanceSchema.GetAttributeByPath("root_block_device.0.iops")),
                new ScalarValue("450"),
                new MappingKey(
                    "kms_key_id",
                    new AttributePath("root_block_device.0.kms_key_id"),
                    Ec2InstanceSchema.GetAttributeByPath("root_block_device.0.kms_key_id")),
                new ScalarValue(string.Empty),
                new MappingKey(
                    "tags",
                    new AttributePath("root_block_device.0.tags"),
                    Ec2InstanceSchema.GetAttributeByPath("root_block_device.0.tags")),
                new MappingStart(),
                new MappingEnd(),
                new MappingKey(
                    "throughput",
                    new AttributePath("root_block_device.0.throughput"),
                    Ec2InstanceSchema.GetAttributeByPath("root_block_device.0.throughput")),
                new ScalarValue("0"),
                new MappingKey(
                    "volume_id",
                    new AttributePath("root_block_device.0.volume_id"),
                    Ec2InstanceSchema.GetAttributeByPath("root_block_device.0.volume_id")),
                new ScalarValue("vol-0b2b0d6c1139c5527"),
                new MappingKey(
                    "volume_size",
                    new AttributePath("root_block_device.0.volume_size"),
                    Ec2InstanceSchema.GetAttributeByPath("root_block_device.0.volume_size")),
                new ScalarValue("150"),
                new MappingKey(
                    "volume_type",
                    new AttributePath("root_block_device.0.volume_type"),
                    Ec2InstanceSchema.GetAttributeByPath("root_block_device.0.volume_type")),
                new ScalarValue("gp2"),
                new MappingEnd(), 
                new SequenceEnd(),
                new MappingKey(
                    "secondary_private_ips",
                    new AttributePath("secondary_private_ips"),
                    Ec2InstanceSchema.GetAttributeByPath("secondary_private_ips")),
                new SequenceStart(),
                new SequenceEnd(),
                new MappingKey(
                    "security_groups",
                    new AttributePath("security_groups"),
                    Ec2InstanceSchema.GetAttributeByPath("security_groups")),
                new SequenceStart(),
                new SequenceEnd(),
                new MappingKey(
                    "source_dest_check",
                    new AttributePath("source_dest_check"),
                    Ec2InstanceSchema.GetAttributeByPath("source_dest_check")),
                new ScalarValue("true"),
                new MappingKey(
                    "subnet_id",
                    new AttributePath("subnet_id"),
                    Ec2InstanceSchema.GetAttributeByPath("subnet_id")),
                new ScalarValue("var.SubnetId"),
                new MappingKey("tags", new AttributePath("tags"), Ec2InstanceSchema.GetAttributeByPath("tags")),
                new MappingStart(),
                new MappingKey("Name", new AttributePath("tags.Name"), Ec2InstanceSchema.GetAttributeByPath("tags.Name")),
                new ScalarValue("my_instance"), 
                new MappingEnd(),
                new MappingKey("tags_all", new AttributePath("tags_all"), Ec2InstanceSchema.GetAttributeByPath("tags_all")),
                new MappingStart(),
                new MappingKey(
                    "Name",
                    new AttributePath("tags_all.Name"),
                    Ec2InstanceSchema.GetAttributeByPath("tags_all.Name")),
                new ScalarValue("my_instance"), 
                new MappingEnd(),
                new MappingKey("tenancy", new AttributePath("tenancy"), Ec2InstanceSchema.GetAttributeByPath("tenancy")),
                new ScalarValue("default"),
                new MappingKey("timeouts", new AttributePath("timeouts"), Ec2InstanceSchema.GetAttributeByPath("timeouts")),
                new MappingStart(),
                new MappingKey(
                    "create",
                    new AttributePath("timeouts.create"),
                    Ec2InstanceSchema.GetAttributeByPath("timeouts.create")),
                new ScalarValue((string)null),
                new MappingKey(
                    "delete",
                    new AttributePath("timeouts.delete"),
                    Ec2InstanceSchema.GetAttributeByPath("timeouts.delete")),
                new ScalarValue((string)null),
                new MappingKey(
                    "update",
                    new AttributePath("timeouts.update"),
                    Ec2InstanceSchema.GetAttributeByPath("timeouts.update")),
                new ScalarValue((string)null), 
                new MappingEnd(),
                new MappingKey(
                    "user_data",
                    new AttributePath("user_data"),
                    Ec2InstanceSchema.GetAttributeByPath("user_data")),
                new ScalarValue((string)null),
                new MappingKey(
                    "user_data_base64",
                    new AttributePath("user_data_base64"),
                    Ec2InstanceSchema.GetAttributeByPath("user_data_base64")),
                new ScalarValue((string)null),
                new MappingKey(
                    "volume_tags",
                    new AttributePath("volume_tags"),
                    Ec2InstanceSchema.GetAttributeByPath("volume_tags")),
                new ScalarValue((string)null),
                new MappingKey(
                    "vpc_security_group_ids",
                    new AttributePath("vpc_security_group_ids"),
                    Ec2InstanceSchema.GetAttributeByPath("vpc_security_group_ids")),
                new SequenceStart(), 
                new ScalarValue("aws_security_group.TorrentSecurityGroup.id"), 
                new SequenceEnd(),
                new MappingEnd(), 
                new ResourceEnd()
            };

        internal static HclEvent[] AutoScalingGroupEvents =
            {
                new ResourceStart("aws_autoscaling_group", "InstanceASG"), 
                new MappingStart(),
                new MappingKey("arn", new AttributePath("arn"), AutoScalingGroupSchema.GetAttributeByPath("arn")),
                new ScalarValue(
                    "arn:aws:autoscaling:eu-west-1:123456789012:autoScalingGroup:49adb3d5-bbcc-4012-8786-dd19806c1d35:autoScalingGroupName/test-asg-1I7FR7OX4Z10G"),
                new MappingKey(
                    "availability_zones",
                    new AttributePath("availability_zones"),
                    AutoScalingGroupSchema.GetAttributeByPath("availability_zones")),
                new SequenceStart(),
                new ScalarValue("eu-west-1a"), 
                new ScalarValue("eu-west-1b"), 
                new SequenceEnd(),
                new MappingKey(
                    "capacity_rebalance",
                    new AttributePath("capacity_rebalance"),
                    AutoScalingGroupSchema.GetAttributeByPath("capacity_rebalance")),
                new ScalarValue("false"),
                new MappingKey(
                    "default_cooldown",
                    new AttributePath("default_cooldown"),
                    AutoScalingGroupSchema.GetAttributeByPath("default_cooldown")),
                new ScalarValue("300"),
                new MappingKey(
                    "desired_capacity",
                    new AttributePath("desired_capacity"),
                    AutoScalingGroupSchema.GetAttributeByPath("desired_capacity")),
                new ScalarValue("1"),
                new MappingKey(
                    "enabled_metrics",
                    new AttributePath("enabled_metrics"),
                    AutoScalingGroupSchema.GetAttributeByPath("enabled_metrics")),
                new SequenceStart(),
                new SequenceEnd(),
                new MappingKey(
                    "force_delete",
                    new AttributePath("force_delete"),
                    AutoScalingGroupSchema.GetAttributeByPath("force_delete")),
                new ScalarValue((string)null),
                new MappingKey(
                    "force_delete_warm_pool",
                    new AttributePath("force_delete_warm_pool"),
                    AutoScalingGroupSchema.GetAttributeByPath("force_delete_warm_pool")),
                new ScalarValue((string)null),
                new MappingKey(
                    "health_check_grace_period",
                    new AttributePath("health_check_grace_period"),
                    AutoScalingGroupSchema.GetAttributeByPath("health_check_grace_period")),
                new ScalarValue("0"),
                new MappingKey(
                    "health_check_type",
                    new AttributePath("health_check_type"),
                    AutoScalingGroupSchema.GetAttributeByPath("health_check_type")),
                new ScalarValue("EC2"),
                new MappingKey("id", new AttributePath("id"), AutoScalingGroupSchema.GetAttributeByPath("id")),
                new ScalarValue("test-asg-1I7FR7OX4Z10G"),
                new MappingKey(
                    "initial_lifecycle_hook",
                    new AttributePath("initial_lifecycle_hook"),
                    AutoScalingGroupSchema.GetAttributeByPath("initial_lifecycle_hook")),
                new SequenceStart(), 
                new SequenceEnd(),
                new MappingKey(
                    "instance_refresh",
                    new AttributePath("instance_refresh"),
                    AutoScalingGroupSchema.GetAttributeByPath("instance_refresh")),
                new SequenceStart(), 
                new SequenceEnd(),
                new MappingKey(
                    "launch_configuration",
                    new AttributePath("launch_configuration"),
                    AutoScalingGroupSchema.GetAttributeByPath("launch_configuration")),
                new ScalarValue("aws_launch_configuration.InstanceLaunchConfig.id"),
                new MappingKey(
                    "launch_template",
                    new AttributePath("launch_template"),
                    AutoScalingGroupSchema.GetAttributeByPath("launch_template")),
                new SequenceStart(), 
                new SequenceEnd(),
                new MappingKey(
                    "load_balancers",
                    new AttributePath("load_balancers"),
                    AutoScalingGroupSchema.GetAttributeByPath("load_balancers")),
                new SequenceStart(),
                new SequenceEnd(),
                new MappingKey(
                    "max_instance_lifetime",
                    new AttributePath("max_instance_lifetime"),
                    AutoScalingGroupSchema.GetAttributeByPath("max_instance_lifetime")),
                new ScalarValue("0"),
                new MappingKey(
                    "max_size",
                    new AttributePath("max_size"),
                    AutoScalingGroupSchema.GetAttributeByPath("max_size")),
                new ScalarValue("1"),
                new MappingKey(
                    "metrics_granularity",
                    new AttributePath("metrics_granularity"),
                    AutoScalingGroupSchema.GetAttributeByPath("metrics_granularity")),
                new ScalarValue("1Minute"),
                new MappingKey(
                    "min_elb_capacity",
                    new AttributePath("min_elb_capacity"),
                    AutoScalingGroupSchema.GetAttributeByPath("min_elb_capacity")),
                new ScalarValue((string)null),
                new MappingKey(
                    "min_size",
                    new AttributePath("min_size"),
                    AutoScalingGroupSchema.GetAttributeByPath("min_size")),
                new ScalarValue("1"),
                new MappingKey(
                    "mixed_instances_policy",
                    new AttributePath("mixed_instances_policy"),
                    AutoScalingGroupSchema.GetAttributeByPath("mixed_instances_policy")),
                new SequenceStart(), 
                new SequenceEnd(),
                new MappingKey("name", new AttributePath("name"), AutoScalingGroupSchema.GetAttributeByPath("name")),
                new ScalarValue("test-asg-1I7FR7OX4Z10G"),
                new MappingKey(
                    "name_prefix",
                    new AttributePath("name_prefix"),
                    AutoScalingGroupSchema.GetAttributeByPath("name_prefix")),
                new ScalarValue(string.Empty),
                new MappingKey(
                    "placement_group",
                    new AttributePath("placement_group"),
                    AutoScalingGroupSchema.GetAttributeByPath("placement_group")),
                new ScalarValue(string.Empty),
                new MappingKey(
                    "protect_from_scale_in",
                    new AttributePath("protect_from_scale_in"),
                    AutoScalingGroupSchema.GetAttributeByPath("protect_from_scale_in")),
                new ScalarValue("false"),
                new MappingKey(
                    "service_linked_role_arn",
                    new AttributePath("service_linked_role_arn"),
                    AutoScalingGroupSchema.GetAttributeByPath("service_linked_role_arn")),
                new ScalarValue(
                    "arn:aws:iam::123456789012:role/aws-service-role/autoscaling.amazonaws.com/AWSServiceRoleForAutoScaling"),
                new MappingKey(
                    "suspended_processes",
                    new AttributePath("suspended_processes"),
                    AutoScalingGroupSchema.GetAttributeByPath("suspended_processes")),
                new SequenceStart(), 
                new SequenceEnd(),
                new MappingKey("tag", new AttributePath("tag"), AutoScalingGroupSchema.GetAttributeByPath("tag")),
                new SequenceStart(), 
                new MappingStart(),
                new MappingKey(
                    "key",
                    new AttributePath("tag.0.key"),
                    AutoScalingGroupSchema.GetAttributeByPath("tag.0.key")),
                new ScalarValue("Name"),
                new MappingKey(
                    "propagate_at_launch",
                    new AttributePath("tag.0.propagate_at_launch"),
                    AutoScalingGroupSchema.GetAttributeByPath("tag.0.propagate_at_launch")),
                new ScalarValue("true"),
                new MappingKey(
                    "value",
                    new AttributePath("tag.0.value"),
                    AutoScalingGroupSchema.GetAttributeByPath("tag.0.value")),
                new ScalarValue("NAT Instance"), 
                new MappingEnd(), 
                new MappingStart(),
                new MappingKey(
                    "key",
                    new AttributePath("tag.0.key"),
                    AutoScalingGroupSchema.GetAttributeByPath("tag.0.key")),
                new ScalarValue("nat:routing:cidr"),
                new MappingKey(
                    "propagate_at_launch",
                    new AttributePath("tag.0.propagate_at_launch"),
                    AutoScalingGroupSchema.GetAttributeByPath("tag.0.propagate_at_launch")),
                new ScalarValue("true"),
                new MappingKey(
                    "value",
                    new AttributePath("tag.0.value"),
                    AutoScalingGroupSchema.GetAttributeByPath("tag.0.value")),
                new ScalarValue("0.0.0.0/0"), 
                new MappingEnd(),
                new SequenceEnd(),
                new MappingKey("tags", new AttributePath("tags"), AutoScalingGroupSchema.GetAttributeByPath("tags")),
                new ScalarValue((string)null),
                new MappingKey(
                    "target_group_arns",
                    new AttributePath("target_group_arns"),
                    AutoScalingGroupSchema.GetAttributeByPath("target_group_arns")),
                new SequenceStart(), 
                new SequenceEnd(),
                new MappingKey(
                    "termination_policies",
                    new AttributePath("termination_policies"),
                    AutoScalingGroupSchema.GetAttributeByPath("termination_policies")),
                new SequenceStart(), 
                new SequenceEnd(),
                new MappingKey(
                    "timeouts",
                    new AttributePath("timeouts"),
                    AutoScalingGroupSchema.GetAttributeByPath("timeouts")),
                new MappingStart(),
                new MappingKey(
                    "delete",
                    new AttributePath("timeouts.delete"),
                    AutoScalingGroupSchema.GetAttributeByPath("timeouts.delete")),
                new ScalarValue((string)null), 
                new MappingEnd(),
                new MappingKey(
                    "vpc_zone_identifier",
                    new AttributePath("vpc_zone_identifier"),
                    AutoScalingGroupSchema.GetAttributeByPath("vpc_zone_identifier")),
                new SequenceStart(), new ScalarValue("subnet-0785b231fa30a41e0"),
                new ScalarValue("subnet-07fc084aea2e3dcee"), 
                new SequenceEnd(),
                new MappingKey(
                    "wait_for_capacity_timeout",
                    new AttributePath("wait_for_capacity_timeout"),
                    AutoScalingGroupSchema.GetAttributeByPath("wait_for_capacity_timeout")),
                new ScalarValue((string)null),
                new MappingKey(
                    "wait_for_elb_capacity",
                    new AttributePath("wait_for_elb_capacity"),
                    AutoScalingGroupSchema.GetAttributeByPath("wait_for_elb_capacity")),
                new ScalarValue((string)null),
                new MappingKey(
                    "warm_pool",
                    new AttributePath("warm_pool"),
                    AutoScalingGroupSchema.GetAttributeByPath("warm_pool")),
                new SequenceStart(),
                new SequenceEnd(), 
                new MappingEnd(), 
                new ResourceEnd()
            };
    }
}
