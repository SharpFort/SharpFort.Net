<template>
    <div class="v-chart" ref="statis"></div>
  </template>
  
  <script setup name="BbsAccessLogChart">
  import { ref } from "vue";
  import useEcharts from "@/hooks/useEcharts";
  import { BbsAccessLogEchartsConfig } from "../../hooks/BbsAccessLogEchartsConfig";
  const props = defineProps({
    option: {
      type: Object,
      default: () => {},
    },
  });
  const emits = defineEmits([
    "chart-click", // 点击chart
  ]);
  
  let statis = ref(null);
  const { resize } = useEcharts(statis, emits, props, BbsAccessLogEchartsConfig);
  defineExpose({
    resize,
  });
  </script>
  <style scoped lang="scss">
  .v-chart {
    width: 100%;
    height: 100%;
  }
  </style>
  