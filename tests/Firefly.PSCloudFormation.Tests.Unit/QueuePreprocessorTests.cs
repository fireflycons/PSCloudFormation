namespace Firefly.PSCloudFormation.Tests.Unit
{
    using System.Linq;

    using Firefly.PSCloudFormation.Terraform.HclSerializer;
    using Firefly.PSCloudFormation.Terraform.HclSerializer.Schema;

    using FluentAssertions;

    using Xunit;

    public class QueuePreprocessorTests
    {
        [Fact]
        public void ShouldRemoveASGEmptyOptionalAttributes()
        {
            var q = new EventQueue(EventQueues.AutoScalingGroupEvents);
            var expectedKeysRemoved = new[]
                                          {
                                              "capacity_rebalance", "enabled_metrics", "force_delete", "force_delete_warm_pool",
                                              "initial_lifecycle_hook", "instance_refresh", "launch_template",
                                              "load_balancers", "min_elb_capacity", "mixed_instances_policy",
                                              "name_prefix", "placement_group", "suspended_processes", "tags",
                                              "target_group_arns", "termination_policies", "timeouts",
                                              "timeouts.delete", "wait_for_capacity_timeout", "wait_for_elb_capacity",
                                              "warm_pool"
                                          };

            var resolver = new EventQueuePreprocessor(q);

            resolver.RemoveEmptyOptionalAttributes();

            q.GetKeys().Select(k => k.Path).Should().NotContain(expectedKeysRemoved);
        }

        [Fact]
        public void ShouldResolveASGConflicts()
        {
            var q = new EventQueue(EventQueues.AutoScalingGroupEvents);
            var traits = AwsSchema.GetResourceTraits("aws_autoscaling_group");

            var resolver = new EventQueuePreprocessor(q);

            resolver.ResolveConflicts();
        }

        [Fact]
        public void ShouldRemoveAllEc2InstanceComputedAttributes()
        {
            var q = new EventQueue(EventQueues.Ec2Events);
            var computedAttributes = new[]
                                         {
                                             "id", "arn", "instance_state", "outpost_arn",
                                             "primary_network_interface_id", "private_dns", "public_dns", "public_ip",
                                             "ebs_block_device.0.volume_id", "root_block_device.0.volume_id", "tags_all"
                                         };
            var resolver = new EventQueuePreprocessor(q);

            resolver.RemoveComputedAttributes();

            q.GetKeys().Select(k => k.Path).Should().NotContain(computedAttributes);
        }
    }
}